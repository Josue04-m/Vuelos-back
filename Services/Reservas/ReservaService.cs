using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Reservas.DTOs;
using Vuelos_Backend.Models.Vuelos;
namespace Vuelos_Backend.Services.Reservas;

public sealed class ReservaService(
    IReservaRepository repository, ICalculadoraTarifas tarifas, TimeProvider clock,
    IOptionsMonitor<OpcionesReservas> opciones, ICicloReservaRepository ciclo,
    IExpiracionReservasService expiracion) : IReservaService
{
    public async Task<ReservaResponse> CrearAsync(
        int usuarioId, CrearReservaRequest request, CancellationToken cancellationToken)
    {
        ValidarEntrada(request);
        foreach (var pasajero in request.Pasajeros) ValidarEntrada(pasajero);
        await expiracion.ExpirarVueloAsync(request.VueloId, cancellationToken);
        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.BloquearVueloAsync(request.VueloId, cancellationToken)
            ?? throw NoEncontrada("vuelo_no_encontrado", "Vuelo no encontrado.");
        ValidarVuelo(vuelo);
        await ciclo.ExpirarPendientesAsync(vuelo.VueloId, clock.GetUtcNow().UtcDateTime, false, cancellationToken);
        if (request.Pasajeros.Count > vuelo.Avion.CapacidadTotal)
            throw Solicitud("pasajeros_exceden_capacidad", "La cantidad de pasajeros supera la capacidad del avión.");
        var ids = request.Pasajeros.Select(p => p.AsientoVueloId).ToArray();
        var asientos = await CargarAsientos(vuelo, ids, cancellationToken);
        tarifas.ValidarConfiguracion();
        var precios = new Dictionary<int, decimal>();
        foreach (var p in request.Pasajeros)
        {
            var asiento = asientos[p.AsientoVueloId];
            ValidarVersion(asiento.RowVersion, p.RowVersionAsiento);
            if (asiento.Estado != "disponible") throw AsientoOcupado();
            precios[p.AsientoVueloId] = tarifas.Calcular(asiento.Precio, p.TipoPasajero);
        }
        var importes = ImportesReserva.Calcular(Total(precios.Values), opciones.CurrentValue.PorcentajeIva);
        var total = importes.Total;
        ValidarTotal(request.TotalEsperado, total);
        var ahora = clock.GetUtcNow().UtcDateTime;
        var vencimiento = ahora.AddMinutes(opciones.CurrentValue.DuracionReservaMinutos);
        var reserva = new Reserva
        {
            CodigoReserva = Convert.ToHexString(RandomNumberGenerator.GetBytes(5)),
            UsuarioId = usuarioId, VueloId = vuelo.VueloId, Vuelo = vuelo,
            FechaReserva = ahora, MontoTotal = total, Subtotal = importes.Subtotal,
            PorcentajeIva = importes.PorcentajeIva, MontoIva = importes.MontoIva, Estado = "pendiente_pago",
            FechaExpiracion = vencimiento < vuelo.SalidaProgramada ? vencimiento : vuelo.SalidaProgramada
        };
        repository.AgregarReserva(reserva);
        foreach (var p in request.Pasajeros.OrderBy(p => p.AsientoVueloId))
        {
            await CambiarEstado(asientos[p.AsientoVueloId], "disponible", "reservado",
                Convert.FromBase64String(p.RowVersionAsiento), cancellationToken);
            repository.AgregarDetalle(new DetalleReserva
            {
                Reserva = reserva, AsientoVueloId = p.AsientoVueloId,
                PrimerNombre = p.PrimerNombre.Trim(), SegundoNombre = Opcional(p.SegundoNombre),
                PrimerApellido = p.PrimerApellido.Trim(), SegundoApellido = Opcional(p.SegundoApellido),
                DocumentoIdentidad = p.Cedula.Trim(), TipoPasajero = p.TipoPasajero,
                PrecioCongelado = precios[p.AsientoVueloId], Estado = "activo"
            });
        }
        await repository.GuardarAsync(cancellationToken);
        var response = await ObtenerPropiaAsync(usuarioId, reserva.ReservaId, cancellationToken);
        ValidarVuelo(vuelo);
        ValidarPlazo(reserva);
        await transaction.ConfirmarAsync(cancellationToken);
        return response;
    }

    public async Task<PaginaReservasResponse> BuscarPropiasAsync(
        int usuarioId, BusquedaReservasRequest request, CancellationToken cancellationToken)
    {
        ValidarEntrada(request);
        if (!ciclo.EnTransaccion) await expiracion.ExpirarLoteAsync(usuarioId, cancellationToken);
        var hasta = request.FechaHasta is { } fin && fin != DateOnly.MaxValue
            ? fin.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : (DateTime?)null;
        var resultado = await repository.BuscarPropiasAsync(usuarioId,
            new FiltroReservas(request.Estado,
                request.FechaDesde?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), hasta,
                request.Pagina, request.TamanoPagina), cancellationToken);
        return new PaginaReservasResponse(resultado.Reservas.Select(r => new ReservaResumenResponse(
            r.ReservaId, r.CodigoReserva, r.VueloId, r.Vuelo.NumeroVuelo, Utc(r.FechaReserva),
            r.MontoTotal, "USD", r.Estado, Convert.ToBase64String(r.RowVersion),
            r.FechaExpiracion.HasValue ? Utc(r.FechaExpiracion.Value) : null, !r.FechaExpiracion.HasValue,
            r.Subtotal, r.PorcentajeIva, r.MontoIva)).ToArray(),
            request.Pagina, request.TamanoPagina, resultado.Total,
            (int)((resultado.Total + (long)request.TamanoPagina - 1) / request.TamanoPagina));
    }

    public async Task<ReservaResponse> ObtenerPropiaAsync(int usuarioId, int reservaId, CancellationToken cancellationToken)
    {
        if (!ciclo.EnTransaccion)
        {
            var vueloId = await repository.ObtenerVueloPropioAsync(reservaId, usuarioId, cancellationToken) ?? throw NoEncontrada();
            await expiracion.ExpirarVueloAsync(vueloId, cancellationToken);
        }
        var datos = await repository.ObtenerPropiaAsync(reservaId, usuarioId, cancellationToken)
            ?? throw NoEncontrada();
        var r = datos.Reserva;
        var compra = await ciclo.ObtenerCompraAsync(reservaId, cancellationToken);
        return new ReservaResponse(r.ReservaId, r.CodigoReserva, r.VueloId, r.Vuelo.NumeroVuelo,
            Utc(r.FechaReserva), r.MontoTotal, "USD", r.Estado, Convert.ToBase64String(r.RowVersion),
            datos.Detalles.Select(d => new DetalleReservaResponse(d.DetalleId, d.AsientoVueloId,
                d.AsientoVuelo.AsientoFisico.CodigoAsiento, d.PrimerNombre, d.SegundoNombre,
                d.PrimerApellido, d.SegundoApellido,
                d.DocumentoIdentidad, d.TipoPasajero, d.PrecioCongelado, d.Estado,
                Convert.ToBase64String(d.AsientoVuelo.RowVersion))).ToArray(),
            datos.Historial.Select(h => new HistorialReservaResponse(h.HistorialReservaId, h.TipoAccion,
                Utc(h.FechaAccion), h.AsientoVueloAnteriorId, h.AsientoVueloNuevoId, h.Observacion)).ToArray(),
            r.FechaExpiracion.HasValue ? Utc(r.FechaExpiracion.Value) : null, !r.FechaExpiracion.HasValue,
            compra is null ? null : new CompraSimuladaResponse(compra.CompraId, Utc(compra.FechaCompra), compra.Monto, compra.Moneda, compra.EsSimulada),
            r.Subtotal, r.PorcentajeIva, r.MontoIva);
    }

    public async Task<ReservaResponse> CambiarAsientosAsync(
        int usuarioId, int reservaId, CambiarAsientosRequest request, CancellationToken cancellationToken)
    {
        ValidarEntrada(request);
        foreach (var cambio in request.Cambios) ValidarEntrada(cambio);
        var vueloId = await repository.ObtenerVueloPropioAsync(reservaId, usuarioId, cancellationToken)
            ?? throw NoEncontrada();
        await expiracion.ExpirarVueloAsync(vueloId, cancellationToken);
        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.BloquearVueloAsync(vueloId, cancellationToken) ?? throw NoEncontrada();
        var reserva = await repository.BloquearPropiaAsync(reservaId, usuarioId, cancellationToken) ?? throw NoEncontrada();
        if (await ciclo.PagoImpideCambiosAsync(reservaId, cancellationToken))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "pago_en_curso", "La reserva tiene una orden PayPal. Finalice el pago o espere su conciliación.");
        await ExigirVigenciaAsync(vuelo, reserva, transaction, cancellationToken);
        ValidarVersion(reserva.RowVersion, request.RowVersion);
        ValidarReservaModificable(reserva);
        ValidarVuelo(vuelo);
        var activos = await repository.ObtenerActivosAsync(reservaId, cancellationToken);
        if (!ImportesReserva.EsConsistente(reserva, Total(activos.Select(d => d.PrecioCongelado))))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "importe_inconsistente", "El desglose de la reserva requiere revisión.");
        var detalles = activos.ToDictionary(d => d.DetalleId);
        if (request.Cambios.Any(c => !detalles.ContainsKey(c.DetalleId)))
            throw Solicitud("detalle_invalido", "Cada cambio debe corresponder a un detalle activo de la reserva.");
        var destinos = request.Cambios.ToDictionary(c => c.DetalleId);
        var idsFinales = activos.Select(d => destinos.TryGetValue(d.DetalleId, out var c) ? c.AsientoVueloId : d.AsientoVueloId).ToArray();
        if (idsFinales.Distinct().Count() != idsFinales.Length)
            throw Solicitud("asientos_repetidos", "La selección final repite un asiento.");
        var cambios = request.Cambios.Where(c => detalles[c.DetalleId].AsientoVueloId != c.AsientoVueloId).ToArray();
        var liberar = cambios.Select(c => detalles[c.DetalleId].AsientoVueloId).ToHashSet();
        var ids = activos.Select(d => d.AsientoVueloId).Concat(request.Cambios.Select(c => c.AsientoVueloId)).Distinct().ToArray();
        var asientos = await CargarAsientos(vuelo, ids, cancellationToken);
        foreach (var c in request.Cambios)
        {
            var destino = asientos[c.AsientoVueloId];
            ValidarVersion(destino.RowVersion, c.RowVersionAsiento);
            var mismoAsiento = detalles[c.DetalleId].AsientoVueloId == c.AsientoVueloId;
            if (destino.Estado != "disponible" &&
                !(destino.Estado == "reservado" && (liberar.Contains(destino.AsientoVueloId) || mismoAsiento)))
                throw AsientoOcupado();
        }
        foreach (var id in liberar)
            if (asientos[id].Estado != "reservado") throw AsientoOcupado();
        if (cambios.Length > 0) tarifas.ValidarConfiguracion();
        var precios = cambios.ToDictionary(c => c.DetalleId,
            c => tarifas.Calcular(asientos[c.AsientoVueloId].Precio, detalles[c.DetalleId].TipoPasajero));
        var subtotal = Total(activos.Select(d => precios.TryGetValue(d.DetalleId, out var precio) ? precio : d.PrecioCongelado));
        // Conservar la tasa de la reserva; los registros antiguos sin desglose mantienen su cálculo anterior.
        ImportesReserva? desglose = reserva.PorcentajeIva.HasValue ? ImportesReserva.Calcular(subtotal, reserva.PorcentajeIva.Value) : null;
        var total = desglose?.Total ?? subtotal;
        ValidarTotal(request.TotalEsperado, total);
        if (cambios.Length == 0)
        {
            var sinCambios = await ObtenerPropiaAsync(usuarioId, reservaId, cancellationToken);
            await transaction.ConfirmarAsync(cancellationToken);
            return sinCambios;
        }

        // Primero se liberan detalles para permitir swaps dentro del índice único.
        // Todo sigue sin confirmar dentro de la misma transacción.
        foreach (var c in cambios) detalles[c.DetalleId].Estado = "liberado";
        await repository.GuardarAsync(cancellationToken);
        foreach (var id in liberar.Order())
            await CambiarEstado(asientos[id], "reservado", "disponible", asientos[id].RowVersion, cancellationToken);
        // Los asientos liberados tienen nuevas versiones generadas por la base.
        var versionesLiberadas = (await repository.ObtenerAsientosAsync(
            vueloId, liberar.ToArray(), cancellationToken)).ToDictionary(a => a.AsientoVueloId);
        foreach (var c in cambios.OrderBy(c => c.AsientoVueloId))
        {
            var version = liberar.Contains(c.AsientoVueloId)
                ? versionesLiberadas[c.AsientoVueloId].RowVersion : Convert.FromBase64String(c.RowVersionAsiento);
            await CambiarEstado(asientos[c.AsientoVueloId], "disponible", "reservado", version, cancellationToken);
            var anterior = detalles[c.DetalleId];
            repository.AgregarDetalle(new DetalleReserva
            {
                Reserva = reserva, ReservaId = reservaId, AsientoVueloId = c.AsientoVueloId,
                PrimerNombre = anterior.PrimerNombre, SegundoNombre = anterior.SegundoNombre,
                PrimerApellido = anterior.PrimerApellido, SegundoApellido = anterior.SegundoApellido,
                DocumentoIdentidad = anterior.DocumentoIdentidad, TipoPasajero = anterior.TipoPasajero,
                PrecioCongelado = precios[c.DetalleId], Estado = "activo"
            });
            repository.AgregarHistorial(new HistorialCambioReserva
            {
                ReservaId = reservaId, TipoAccion = "CAMBIO_ASIENTO",
                AsientoVueloAnteriorId = anterior.AsientoVueloId, AsientoVueloNuevoId = c.AsientoVueloId,
                FechaAccion = clock.GetUtcNow().UtcDateTime
            });
        }
        reserva.MontoTotal = total;
        desglose?.Aplicar(reserva);
        if (reserva.Estado != "pendiente_pago") reserva.Estado = "modificada";
        repository.PrepararVersionReserva(reserva, Convert.FromBase64String(request.RowVersion));
        await repository.GuardarAsync(cancellationToken);
        var response = await ObtenerPropiaAsync(usuarioId, reservaId, cancellationToken);
        ValidarVuelo(vuelo);
        ValidarPlazo(reserva);
        await transaction.ConfirmarAsync(cancellationToken);
        return response;
    }

    public async Task<ReservaResponse> CancelarAsync(
        int usuarioId, int reservaId, CancelarReservaRequest request, CancellationToken cancellationToken)
    {
        ValidarEntrada(request);
        var vueloId = await repository.ObtenerVueloPropioAsync(reservaId, usuarioId, cancellationToken)
            ?? throw NoEncontrada();
        await expiracion.ExpirarVueloAsync(vueloId, cancellationToken);
        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.BloquearVueloAsync(vueloId, cancellationToken) ?? throw NoEncontrada();
        var reserva = await repository.BloquearPropiaAsync(reservaId, usuarioId, cancellationToken) ?? throw NoEncontrada();
        if (await ciclo.PagoImpideCambiosAsync(reservaId, cancellationToken, soloCaptura: true))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "pago_en_curso", "La reserva tiene una orden PayPal. Finalice el pago o espere su conciliación.");
        await ExigirVigenciaAsync(vuelo, reserva, transaction, cancellationToken);
        ValidarVersion(reserva.RowVersion, request.RowVersion);
        if (reserva.Estado == "cancelada")
        {
            var cancelada = await ObtenerPropiaAsync(usuarioId, reservaId, cancellationToken);
            await transaction.ConfirmarAsync(cancellationToken);
            return cancelada;
        }
        ValidarReservaModificable(reserva);
        ValidarVuelo(vuelo);
        var activos = await repository.ObtenerActivosAsync(reservaId, cancellationToken);
        var asientos = await CargarAsientos(vuelo, activos.Select(d => d.AsientoVueloId).ToArray(), cancellationToken);
        foreach (var d in activos.OrderBy(d => d.AsientoVueloId))
        {
            await CambiarEstado(asientos[d.AsientoVueloId], "reservado", "disponible",
                asientos[d.AsientoVueloId].RowVersion, cancellationToken);
            d.Estado = "liberado";
        }
        reserva.Estado = "cancelada"; // El importe congelado se conserva como historial.
        repository.PrepararVersionReserva(reserva, Convert.FromBase64String(request.RowVersion));
        repository.AgregarHistorial(new HistorialCambioReserva
        {
            ReservaId = reservaId, TipoAccion = "CANCELACION", FechaAccion = clock.GetUtcNow().UtcDateTime
        });
        await repository.GuardarAsync(cancellationToken);
        var response = await ObtenerPropiaAsync(usuarioId, reservaId, cancellationToken);
        ValidarVuelo(vuelo);
        await transaction.ConfirmarAsync(cancellationToken);
        return response;
    }

    private async Task ExigirVigenciaAsync(Vuelo vuelo, Reserva reserva, ITransaccionReserva transaction, CancellationToken ct)
    {
        var ahora = clock.GetUtcNow().UtcDateTime;
        var cerrado = vuelo.Estado is not ("programado" or "retrasado") || vuelo.SalidaReal.HasValue || vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= ahora;
        if (reserva.Estado == "pendiente_pago" && (reserva.FechaExpiracion <= ahora || cerrado))
        {
            await ciclo.ExpirarPendientesAsync(vuelo.VueloId, ahora, cerrado, ct);
            await transaction.ConfirmarAsync(ct); // Conservar la liberación aunque la compra/modificación se rechace.
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "reserva_expirada", "La reserva expiró y sus asientos fueron liberados.");
        }
        if (reserva.Estado == "expirada")
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "reserva_expirada", "La reserva expiró. Debe crear una nueva.");
    }

    private void ValidarPlazo(Reserva reserva)
    {
        if (reserva.FechaExpiracion.HasValue && reserva.FechaExpiracion <= clock.GetUtcNow().UtcDateTime)
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "reserva_expirada", "La operación excedió el plazo de compra. Consulte la reserva.");
    }

    private async Task<Dictionary<int, AsientoVuelo>> CargarAsientos(Vuelo vuelo, int[] ids, CancellationToken cancellationToken)
    {
        var asientos = await repository.ObtenerAsientosAsync(vuelo.VueloId, ids, cancellationToken);
        if (asientos.Count != ids.Distinct().Count() ||
            asientos.Any(a => a.AsientoFisico.AvionId != vuelo.AvionId))
            throw Solicitud("asientos_invalidos", "Todos los asientos deben pertenecer al vuelo y a su avión.");
        return asientos.ToDictionary(a => a.AsientoVueloId);
    }

    private async Task CambiarEstado(AsientoVuelo asiento, string anterior, string nuevo,
        byte[] version, CancellationToken cancellationToken)
    {
        if (await repository.CambiarEstadoAsientoAsync(asiento.AsientoVueloId, asiento.VueloId,
            anterior, nuevo, version, cancellationToken) != 1)
            throw AsientoOcupado();
    }

    private void ValidarVuelo(Vuelo vuelo)
    {
        if (!(vuelo.Estado is "programado" or "retrasado") || vuelo.SalidaReal.HasValue ||
            vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= clock.GetUtcNow().UtcDateTime)
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "vuelo_no_reservable",
                "El vuelo ya no permite confirmar, modificar o cancelar reservas.");
    }

    private static void ValidarReservaModificable(Reserva reserva)
    {
        if (reserva.Estado == "pagada")
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "compra_no_modificable", "Las compras no admiten cambios ni cancelaciones del cliente en este laboratorio.");
        if (reserva.Estado is not ("confirmada" or "modificada" or "pendiente_pago"))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "reserva_no_modificable", "La reserva ya no permite modificaciones.");
    }
    private static void ValidarVersion(byte[] actual, string original)
    {
        if (!actual.SequenceEqual(Convert.FromBase64String(original)))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "version_desactualizada", "La información cambió. Consulte la versión vigente.");
    }
    private static void ValidarTotal(decimal esperado, decimal actual)
    {
        if (esperado != actual)
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "tarifa_actualizada",
                "El total vigente difiere del esperado. Confirme nuevamente el importe en USD.", actual);
    }
    private static decimal Total(IEnumerable<decimal> precios)
    {
        var total = precios.Sum();
        if (total > 99999999.99m)
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "importe_fuera_de_rango", "El total supera el importe admitido por el esquema.");
        return total;
    }
    private static void ValidarEntrada(object request)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errores, true))
            throw Solicitud("datos_invalidos", string.Join(" ", errores.Select(e => e.ErrorMessage).Distinct()));
    }
    private static ReservaOperacionException Solicitud(string codigo, string mensaje) =>
        new(ErrorReserva.SolicitudInvalida, codigo, mensaje);
    private static ReservaOperacionException NoEncontrada(
        string codigo = "reserva_no_encontrada", string mensaje = "Reserva no encontrada.") =>
        new(ErrorReserva.NoEncontrada, codigo, mensaje);
    private static ReservaOperacionException AsientoOcupado() =>
        new(ErrorReserva.Conflicto, "asiento_no_disponible", "Un asiento ya no está disponible o cambió su versión.");
    private static DateTimeOffset Utc(DateTime fecha) => new(DateTime.SpecifyKind(fecha, DateTimeKind.Utc));
    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
