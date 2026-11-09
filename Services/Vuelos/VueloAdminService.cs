using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;
using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Services.Vuelos;

public sealed class VueloAdminService(IVueloAdminRepository repository, TimeProvider clock) : IVueloAdminService
{
    private static readonly Dictionary<string, HashSet<string>> TransicionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["programado"] = ["embarcando", "retrasado", "cancelado"],
            ["retrasado"] = ["programado", "embarcando", "cancelado"],
            ["embarcando"] = ["en vuelo", "cancelado"],
            ["en vuelo"] = ["finalizado"],
            ["finalizado"] = [],
            ["cancelado"] = []
        };

    public async Task<VueloAdminResponse> ObtenerAsync(int vueloId, CancellationToken cancellationToken)
    {
        var vuelo = await repository.ObtenerAsync(vueloId, false, cancellationToken) ?? throw NoEncontrado();
        return Respuesta(vuelo, await repository.ContarAsientosAsync(vueloId, cancellationToken));
    }

    public async Task<VueloAdminResponse> CrearAsync(CrearVueloAdminRequest request, int administradorId,
        CancellationToken cancellationToken)
    {
        Validar(request);
        if (administradorId <= 0) throw Invalido("Administrador inválido.");
        ValidarProgramacion(request.AeropuertoOrigenId, request.AeropuertoDestinoId,
            request.SalidaProgramada, request.LlegadaProgramada);
        ValidarTarifas(request.TarifasPorClase);

        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var avion = await repository.BloquearAvionAsync(request.AvionId, cancellationToken)
            ?? throw Invalido("El avión indicado no existe.");
        if (!avion.Activo) throw Invalido("El avión está desactivado.");
        await ValidarAeropuertos(request.AeropuertoOrigenId, request.AeropuertoDestinoId, cancellationToken);
        var fisicos = await repository.ObtenerAsientosFisicosAsync(request.AvionId, cancellationToken);
        if (avion.CapacidadTotal <= 0 || fisicos.Count != avion.CapacidadTotal)
            throw Conflicto("asientos_avion_incompletos", "El avión debe tener sus asientos físicos configurados según su capacidad.");
        var precios = PreciosPorClase(fisicos, request.TarifasPorClase);
        await ValidarSolapamiento(request.AvionId, null, request.SalidaProgramada, request.LlegadaProgramada, cancellationToken);
        var ahora = clock.GetUtcNow().UtcDateTime;
        // Revalidar tras esperar los bloqueos: el vuelo debe seguir siendo futuro.
        ValidarProgramacion(request.AeropuertoOrigenId, request.AeropuertoDestinoId, request.SalidaProgramada, request.LlegadaProgramada);
        var vuelo = new Vuelo
        {
            NumeroVuelo = request.NumeroVuelo.Trim().ToUpperInvariant(), AvionId = request.AvionId,
            AeropuertoOrigenId = request.AeropuertoOrigenId, AeropuertoDestinoId = request.AeropuertoDestinoId,
            SalidaProgramada = request.SalidaProgramada.UtcDateTime, LlegadaProgramada = request.LlegadaProgramada.UtcDateTime,
            Estado = "programado", FechaCreacion = ahora
        };
        var asientos = fisicos.Select(a => new AsientoVuelo
        {
            Vuelo = vuelo, AsientoFisicoId = a.AsientoFisicoId, Precio = precios[a.CategoriaId], Estado = "disponible"
        }).ToArray();
        repository.Agregar(vuelo, asientos, new HistorialEstadoVuelo
        {
            Vuelo = vuelo, EstadoAnterior = null, EstadoNuevo = "programado", FechaCambio = ahora,
            UsuarioAdminId = administradorId, Observacion = "Creación del vuelo."
        });
        await repository.GuardarAsync(cancellationToken);
        // Leer antes del commit evita responder con una edición concurrente posterior a nuestra creación.
        var resultado = await ObtenerAsync(vuelo.VueloId, cancellationToken);
        await transaction.ConfirmarAsync(cancellationToken);
        return resultado;
    }

    public async Task<VueloAdminResponse> EditarAsync(int vueloId, EditarVueloAdminRequest request,
        CancellationToken cancellationToken)
    {
        Validar(request);
        ValidarProgramacion(request.AeropuertoOrigenId, request.AeropuertoDestinoId,
            request.SalidaProgramada, request.LlegadaProgramada);
        var avionId = await repository.ObtenerAvionIdAsync(vueloId, cancellationToken) ?? throw NoEncontrado();
        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var avion = await repository.BloquearAvionAsync(avionId, cancellationToken);
        if (avion is null || !avion.Activo) throw Invalido("El avión no está disponible para programación.");
        // Mismo bloqueo y orden de vuelo antes de reservas que ReservaRepository.
        var vuelo = await repository.ObtenerAsync(vueloId, true, cancellationToken) ?? throw NoEncontrado();
        if (!string.Equals(Version(vuelo), request.Version, StringComparison.OrdinalIgnoreCase))
            throw Conflicto("version_desactualizada", "El vuelo cambió. Consulte su versión vigente antes de editarlo.");
        if (vuelo.Estado is not ("programado" or "retrasado") || vuelo.SalidaReal.HasValue ||
            vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= clock.GetUtcNow().UtcDateTime)
            throw Conflicto("vuelo_no_editable", "Solo puede editar la programación de vuelos futuros sin iniciar operaciones.");
        await repository.ExpirarPendientesAsync(vueloId, clock.GetUtcNow().UtcDateTime, false, cancellationToken);
        if (await repository.TieneReservasRegistradasAsync(vueloId, cancellationToken))
            throw Conflicto("vuelo_con_reservas", "El vuelo tiene reservas pendientes vigentes, compras registradas o reservas anteriores al flujo temporal.");
        await ValidarAeropuertos(request.AeropuertoOrigenId, request.AeropuertoDestinoId, cancellationToken);
        await ValidarSolapamiento(avionId, vueloId, request.SalidaProgramada, request.LlegadaProgramada, cancellationToken);
        ValidarProgramacion(request.AeropuertoOrigenId, request.AeropuertoDestinoId, request.SalidaProgramada, request.LlegadaProgramada);
        vuelo.NumeroVuelo = request.NumeroVuelo.Trim().ToUpperInvariant();
        vuelo.AeropuertoOrigenId = request.AeropuertoOrigenId;
        vuelo.AeropuertoDestinoId = request.AeropuertoDestinoId;
        vuelo.SalidaProgramada = request.SalidaProgramada.UtcDateTime;
        vuelo.LlegadaProgramada = request.LlegadaProgramada.UtcDateTime;
        await repository.GuardarAsync(cancellationToken);
        var resultado = await ObtenerAsync(vueloId, cancellationToken);
        await transaction.ConfirmarAsync(cancellationToken);
        return resultado;
    }

    public async Task<VueloAdminResponse> CambiarAvionAsync(int vueloId, CambiarAvionVueloRequest request, int administradorId, CancellationToken ct)
    {
        Validar(request);
        ValidarTarifas(request.TarifasPorClase);
        if (administradorId <= 0) throw Invalido("Administrador inválido.");
        await using var transaction = await repository.IniciarTransaccionAsync(ct);
        // El bloqueo administrativo común se adquiere antes de cualquier bloqueo de avión/vuelo.
        var avion = await repository.BloquearAvionAsync(request.AvionId, ct) ?? throw Invalido("El avión no existe.");
        if (!avion.Activo) throw Invalido("El avión está desactivado.");
        var vuelo = await repository.ObtenerAsync(vueloId, true, ct) ?? throw NoEncontrado();
        if (!string.Equals(Version(vuelo), request.Version, StringComparison.OrdinalIgnoreCase))
            throw Conflicto("version_desactualizada", "Consulte la versión vigente del vuelo.");
        if (vuelo.AvionId == request.AvionId) throw Invalido("Seleccione un avión diferente al actual.");
        if (vuelo.Estado is not ("programado" or "retrasado") || vuelo.SalidaReal.HasValue || vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= clock.GetUtcNow().UtcDateTime)
            throw Conflicto("vuelo_no_editable", "Solo puede cambiar el avión antes de iniciar operaciones y antes de su salida.");
        await repository.ExpirarPendientesAsync(vueloId, clock.GetUtcNow().UtcDateTime, false, ct);
        if (await repository.TieneReservasRegistradasAsync(vueloId, ct))
            throw Conflicto("vuelo_con_reservas", "No puede cambiar el avión con reservas pendientes vigentes, compras registradas o reservas anteriores al flujo temporal.");
        var fisicos = await repository.ObtenerAsientosFisicosAsync(request.AvionId, ct);
        if (avion.CapacidadTotal <= 0 || fisicos.Count != avion.CapacidadTotal)
            throw Conflicto("asientos_avion_incompletos", "El avión debe tener todos sus asientos y categorías activos.");
        var precios = PreciosPorClase(fisicos, request.TarifasPorClase);
        await ValidarSolapamiento(request.AvionId, vueloId, new DateTimeOffset(DateTime.SpecifyKind(vuelo.SalidaProgramada, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(vuelo.LlegadaProgramada, DateTimeKind.Utc)), ct);
        var existentes = await repository.ObtenerTodosAsientosAsync(vueloId, ct);
        if (existentes.Any(a => a.Estado == "reservado"))
            throw Conflicto("vuelo_con_reservas", "El vuelo contiene asientos reservados y requiere revisión.");
        // Conservar las filas anteriores y sus claves; las consultas operativas solo muestran el avión actual.
        foreach (var asiento in existentes) asiento.Estado = "bloqueado";
        var nuevos = new List<AsientoVuelo>();
        foreach (var fisico in fisicos)
        {
            var asiento = existentes.SingleOrDefault(a => a.AsientoFisicoId == fisico.AsientoFisicoId);
            if (asiento is null)
            {
                asiento = new AsientoVuelo { VueloId = vueloId, AsientoFisicoId = fisico.AsientoFisicoId };
                nuevos.Add(asiento);
            }
            asiento.Precio = precios[fisico.CategoriaId];
            asiento.Estado = "disponible";
        }
        var anterior = vuelo.AvionId;
        vuelo.AvionId = request.AvionId;
        repository.AgregarAsientos(nuevos);
        repository.AgregarHistorial(new HistorialEstadoVuelo {
            VueloId = vueloId, EstadoAnterior = vuelo.Estado, EstadoNuevo = vuelo.Estado,
            FechaCambio = clock.GetUtcNow().UtcDateTime, UsuarioAdminId = administradorId,
            Observacion = $"Cambio de avión {anterior} → {request.AvionId}; asientos y tarifas por clase regenerados."
        });
        if (vuelo.SalidaProgramada <= clock.GetUtcNow().UtcDateTime)
            throw Conflicto("vuelo_no_editable", "El vuelo ya alcanzó su hora de salida.");
        await repository.GuardarAsync(ct);
        var resultado = await ObtenerAsync(vueloId, ct);
        await transaction.ConfirmarAsync(ct);
        return resultado;
    }

    private static void ValidarTarifas(IReadOnlyList<PrecioClaseAdminRequest> tarifas)
    {
        if (tarifas is null || tarifas.Count == 0 || tarifas.Any(t => t is null)) throw Invalido("Indique un precio por cada clase del avión.");
        foreach (var tarifa in tarifas) Validar(tarifa);
        if (tarifas.Select(t => t.CategoriaId).Distinct().Count() != tarifas.Count) throw Invalido("No repita categorías en tarifasPorClase.");
    }

    private static Dictionary<int, decimal> PreciosPorClase(IReadOnlyList<AsientoFisico> fisicos, IReadOnlyList<PrecioClaseAdminRequest> tarifas)
    {
        if (!fisicos.Select(a => a.CategoriaId).Distinct().Order().SequenceEqual(tarifas.Select(t => t.CategoriaId).Order()))
            throw Invalido("Debe indicar exactamente un precio para cada categoría presente en el avión.");
        return tarifas.ToDictionary(t => t.CategoriaId, t => t.Precio);
    }

    public async Task<VueloAdminResponse> CambiarEstadoAsync(int vueloId, CambiarEstadoVueloAdminRequest request,
        int administradorId, CancellationToken cancellationToken)
    {
        Validar(request);
        if (administradorId <= 0) throw Invalido("Administrador inválido.");

        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.ObtenerAsync(vueloId, true, cancellationToken) ?? throw NoEncontrado();

        if (!string.Equals(Version(vuelo), request.Version, StringComparison.OrdinalIgnoreCase))
            throw Conflicto("version_desactualizada", "El vuelo cambió. Consulte su versión vigente antes de modificar su estado.");

        var estadoActual = vuelo.Estado;
        var nuevoEstado = request.NuevoEstado.Trim().ToLowerInvariant();

        if (string.Equals(estadoActual, nuevoEstado, StringComparison.OrdinalIgnoreCase))
            throw Conflicto("estado_sin_cambio", $"El vuelo ya se encuentra en el estado '{estadoActual}'.");

        if (!TransicionesPermitidas.TryGetValue(estadoActual, out var permitidos) || !permitidos.Contains(nuevoEstado))
            throw Conflicto("transicion_invalida", $"No se permite la transición del estado '{estadoActual}' al estado '{nuevoEstado}'.");

        var ahora = clock.GetUtcNow().UtcDateTime;

        if (request.SalidaReal.HasValue && nuevoEstado != "en vuelo" ||
            request.LlegadaReal.HasValue && nuevoEstado != "finalizado")
            throw Invalido("La salida real solo corresponde a 'en vuelo' y la llegada real a 'finalizado'.");

        if (nuevoEstado == "en vuelo")
        {
            // La implementación anterior registraba salida al embarcar. No reinterpretar ni sobrescribir esos datos.
            if (vuelo.SalidaReal.HasValue || vuelo.LlegadaReal.HasValue)
                throw Conflicto("horarios_requieren_revision", "El vuelo tiene horas reales previas al despegue. Requiere revisión de sus datos históricos.");
            var salida = request.SalidaReal?.UtcDateTime ?? ahora;
            if (salida > ahora)
                throw Invalido("La salida real no puede ser futura.");
            vuelo.SalidaReal = salida;
        }

        if (nuevoEstado == "finalizado")
        {
            if (!vuelo.SalidaReal.HasValue || vuelo.SalidaReal > ahora || vuelo.LlegadaReal.HasValue)
                throw Conflicto("horarios_requieren_revision", "Las horas reales existentes requieren revisión antes de finalizar el vuelo.");
            var llegada = request.LlegadaReal?.UtcDateTime ?? ahora;
            if (llegada > ahora)
                throw Invalido("La llegada real no puede ser futura.");
            if (llegada <= vuelo.SalidaReal.Value)
                throw Invalido("La llegada real debe ser posterior a la salida real.");
            vuelo.LlegadaReal = llegada;
        }

        if (nuevoEstado is "cancelado")
        {
            await repository.CancelarReservasYLiberarAsientosAsync(vueloId, ahora, cancellationToken);
        }

        if (nuevoEstado is "embarcando" or "en vuelo" or "finalizado")
            await repository.ExpirarPendientesAsync(vueloId, ahora, true, cancellationToken);
        vuelo.Estado = nuevoEstado;
        repository.AgregarHistorial(new HistorialEstadoVuelo
        {
            VueloId = vueloId,
            EstadoAnterior = estadoActual,
            EstadoNuevo = nuevoEstado,
            FechaCambio = ahora,
            UsuarioAdminId = administradorId,
            Observacion = request.Observacion
        });

        await repository.GuardarAsync(cancellationToken);
        var resultado = await ObtenerAsync(vueloId, cancellationToken);
        await transaction.ConfirmarAsync(cancellationToken);
        return resultado;
    }

    public async Task<IReadOnlyList<HistorialEstadoVueloAdminResponse>> ObtenerHistorialAsync(
        int vueloId, CancellationToken cancellationToken)
    {
        _ = await repository.ObtenerAsync(vueloId, false, cancellationToken) ?? throw NoEncontrado();
        var historiales = await repository.ObtenerHistorialAsync(vueloId, cancellationToken);

        return historiales.Select(h => new HistorialEstadoVueloAdminResponse(
            h.HistorialId,
            h.VueloId,
            h.EstadoAnterior,
            h.EstadoNuevo,
            new DateTimeOffset(h.FechaCambio, TimeSpan.Zero),
            h.UsuarioAdminId,
            h.UsuarioAdmin != null ? $"{h.UsuarioAdmin.Nombre} {h.UsuarioAdmin.Apellido}" : null,
            h.Observacion
        )).ToList();
    }

    public async Task<IReadOnlyList<AsientoVueloAdminResponse>> ListarAsientosAsync(
        int vueloId, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.ObtenerAsync(vueloId, true, cancellationToken) ?? throw NoEncontrado();
        var ahora = clock.GetUtcNow().UtcDateTime;
        var cerrado = vuelo.Estado is not ("programado" or "retrasado") || vuelo.SalidaReal.HasValue || vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= ahora;
        await repository.ExpirarPendientesAsync(vueloId, ahora, cerrado, cancellationToken);
        var asientos = await repository.ListarAsientosConDetallesAsync(vueloId, cancellationToken);
        var activos = await repository.ObtenerIdsAsientosConReservaActivaAsync(vueloId, cancellationToken);

        var response = asientos.Select(a => new AsientoVueloAdminResponse(
            a.AsientoVueloId,
            a.AsientoFisicoId,
            a.AsientoFisico.CodigoAsiento,
            a.AsientoFisico.Categoria?.Nombre ?? string.Empty,
            a.Precio,
            a.Estado,
            Convert.ToBase64String(a.RowVersion),
            activos.Contains(a.AsientoVueloId)
        )).ToList();
        await transaction.ConfirmarAsync(cancellationToken);
        return response;
    }

    public async Task<AsientoVueloAdminResponse> ActualizarAsientoAsync(
        int vueloId, int asientoVueloId, ActualizarAsientoVueloAdminRequest request, CancellationToken cancellationToken)
    {
        Validar(request);
        byte[] versionEsperada = Convert.FromBase64String(request.RowVersion);

        await using var transaction = await repository.IniciarTransaccionAsync(cancellationToken);
        var vuelo = await repository.ObtenerAsync(vueloId, true, cancellationToken) ?? throw NoEncontrado();

        if (vuelo.Estado is "cancelado" or "finalizado")
            throw Conflicto("vuelo_no_modificable", "No se pueden modificar asientos de un vuelo cancelado o finalizado.");

        var asiento = await repository.ObtenerAsientoVueloAsync(vueloId, asientoVueloId, true, cancellationToken)
            ?? throw NoEncontrado("Asiento no encontrado en este vuelo.");

        if (!asiento.RowVersion.SequenceEqual(versionEsperada))
            throw Conflicto("version_desactualizada", "El asiento cambió concurrentemente. Consulte la versión actual antes de modificarlo.");

        bool tieneReservaActiva = await repository.AsientoTieneReservaActivaAsync(asientoVueloId, cancellationToken);

        if (request.Estado is not null)
        {
            if (asiento.Estado == "reservado" || tieneReservaActiva)
                throw Conflicto("asiento_con_reserva", "No se puede cambiar la disponibilidad de un asiento que tiene una reserva activa.");

            asiento.Estado = request.Estado;
        }

        if (request.Precio.HasValue)
        {
            asiento.Precio = request.Precio.Value;
        }

        await repository.GuardarAsync(cancellationToken);

        var response = new AsientoVueloAdminResponse(
            asiento.AsientoVueloId,
            asiento.AsientoFisicoId,
            asiento.AsientoFisico.CodigoAsiento,
            asiento.AsientoFisico.Categoria?.Nombre ?? string.Empty,
            asiento.Precio,
            asiento.Estado,
            Convert.ToBase64String(asiento.RowVersion),
            tieneReservaActiva
        );

        await transaction.ConfirmarAsync(cancellationToken);
        return response;
    }

    public async Task<IReadOnlyList<ReservaVueloAdminResponse>> ListarReservasAsync(
        int vueloId, CancellationToken cancellationToken)
    {
        _ = await repository.ObtenerAsync(vueloId, false, cancellationToken) ?? throw NoEncontrado();
        var reservas = await repository.ObtenerReservasDeVueloAsync(vueloId, cancellationToken);
        var detalles = await repository.ObtenerDetallesDeReservasAsync(reservas.Select(r => r.ReservaId), cancellationToken);

        var detallesPorReserva = detalles.GroupBy(d => d.ReservaId).ToDictionary(g => g.Key, g => g.ToList());

        return reservas.Select(r =>
        {
            var dets = detallesPorReserva.TryGetValue(r.ReservaId, out var list) ? list : [];
            var pasajeros = dets.Select(d => new PasajeroVueloAdminResponse(
                d.DetalleId,
                d.PrimerNombre,
                d.SegundoNombre,
                d.PrimerApellido,
                d.SegundoApellido,
                d.DocumentoIdentidad,
                d.TipoPasajero,
                d.AsientoVuelo?.AsientoFisico?.CodigoAsiento ?? string.Empty,
                d.PrecioCongelado,
                d.Estado
            )).ToList();

            return new ReservaVueloAdminResponse(
                r.ReservaId,
                r.CodigoReserva,
                r.Estado,
                r.MontoTotal,
                new DateTimeOffset(r.FechaReserva, TimeSpan.Zero),
                r.UsuarioId,
                r.Usuario != null ? $"{r.Usuario.Nombre} {r.Usuario.Apellido}" : string.Empty,
                r.Usuario?.Email ?? string.Empty,
                pasajeros, r.Subtotal, r.PorcentajeIva, r.MontoIva
            );
        }).ToList();
    }

    private async Task ValidarAeropuertos(int origen, int destino, CancellationToken ct)
    {
        if (!await repository.ExisteAeropuertoAsync(origen, ct) || !await repository.ExisteAeropuertoAsync(destino, ct))
            throw Invalido("El aeropuerto de origen o destino no existe.");
    }

    private async Task ValidarSolapamiento(int avionId, int? vueloId, DateTimeOffset salida, DateTimeOffset llegada, CancellationToken ct)
    {
        if (await repository.TieneSolapamientoAsync(avionId, vueloId, salida.UtcDateTime, llegada.UtcDateTime, ct))
            throw Conflicto("avion_no_disponible", "El avión ya tiene otro vuelo durante ese intervalo.");
    }

    private void ValidarProgramacion(int origen, int destino, DateTimeOffset salida, DateTimeOffset llegada)
    {
        if (origen == destino) throw Invalido("Origen y destino deben ser diferentes.");
        if (salida <= clock.GetUtcNow() || llegada <= salida)
            throw Invalido("La salida debe ser futura y la llegada posterior a la salida.");
    }

    private static void Validar(object request)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errores, true))
            throw Invalido(errores[0].ErrorMessage ?? "Solicitud inválida.");
    }

    // Token del contenido de programación, no una columna SQL rowversion ni una credencial.
    private static string Version(Vuelo v) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        v.VueloId, v.NumeroVuelo, v.AvionId, v.AeropuertoOrigenId, v.AeropuertoDestinoId,
        Salida = v.SalidaProgramada.Ticks, Llegada = v.LlegadaProgramada.Ticks,
        SalidaReal = v.SalidaReal?.Ticks, LlegadaReal = v.LlegadaReal?.Ticks, v.Estado
    })));

    private VueloAdminResponse Respuesta(Vuelo vuelo, int asientos) =>
        new(VueloService.Mapear(vuelo, clock.GetUtcNow().UtcDateTime), Version(vuelo), asientos);
    private static VueloAdminOperacionException NoEncontrado(string mensaje = "Vuelo no encontrado.") =>
        new(ErrorVueloAdmin.NoEncontrado, "vuelo_no_encontrado", mensaje);
    private static VueloAdminOperacionException Invalido(string mensaje) =>
        new(ErrorVueloAdmin.SolicitudInvalida, "solicitud_vuelo_invalida", mensaje);
    private static VueloAdminOperacionException Conflicto(string codigo, string mensaje) =>
        new(ErrorVueloAdmin.Conflicto, codigo, mensaje);
}
