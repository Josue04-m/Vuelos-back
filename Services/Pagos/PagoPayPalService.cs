using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Pagos;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Pagos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Services.Pagos;

public sealed class PagoPayPalService(IPagoPayPalRepository store, IReservaRepository reservas, IPayPalClient paypal,
    IOptions<OpcionesPayPal> opciones, TimeProvider clock)
{
    private DateTime Ahora => clock.GetUtcNow().UtcDateTime;
    private OpcionesPayPal Config => opciones.Value;

    public async Task<PagoPayPalResponse> CrearAsync(int usuario, int id, CrearOrdenPayPalRequest request, CancellationToken ct)
    {
        Config.Validar();
        var token = Guid.NewGuid();
        var pago = await Bloqueo(usuario, id, async (r, v) =>
        {
            var p = await store.PorReservaAsync(id, ct);
            if (p is not null)
            {
                Entorno(p);
                if (p.OrdenId is not null || p.OperacionHasta > Ahora) return p;
                if (p.Estado != "creando") throw Conflicto("pago_no_reiniciable", "Consulte el pago existente; no se crea un segundo cobro.");
            }
            await ExigirComprable(r, v, ct);
            byte[] version;
            try { version = Convert.FromBase64String(request.RowVersion); }
            catch (FormatException) { throw Conflicto("version_invalida", "Consulte la versión vigente de la reserva."); }
            if (!r.RowVersion.SequenceEqual(version)) throw Conflicto("version_desactualizada", "Consulte la versión vigente de la reserva.");
            if (p is null)
            {
                p = new PagoPayPal { PagoId = Guid.NewGuid(), ReservaId = id, Monto = r.MontoTotal,
                    Entorno = Config.Entorno, MerchantId = Config.MerchantId, FechaCreacion = Ahora };
                store.Agregar(p);
            }
            p.OperacionToken = token;
            p.OperacionHasta = Ahora.AddSeconds(90);
            p.FechaActualizacion = Ahora;
            return p;
        }, ct);
        if (pago.OperacionToken != token) return await LeerAsync(usuario, id, ct);
        // No hay transacción SQL durante las llamadas HTTP.
        var orden = await paypal.CrearAsync(pago, ct);
        await Bloqueo(usuario, id, async (r, v) =>
        {
            var p = await store.PorReservaAsync(id, ct) ?? throw NoEncontrado();
            if (p.OperacionToken != token) return p;
            ValidarOrden(p, orden, false);
            p.OrdenId = orden.Id;
            p.UrlAprobacion = orden.UrlAprobacion;
            p.Estado = "creada";
            LiberarOperacion(p);
            return p;
        }, ct);
        return await LeerAsync(usuario, id, ct);
    }

    public async Task<PagoPayPalResponse> CapturarAsync(int usuario, int id, CancellationToken ct)
    {
        Config.Validar();
        var token = Guid.NewGuid();
        var pago = await Bloqueo(usuario, id, async (r, v) =>
        {
            var p = await store.PorReservaAsync(id, ct) ?? throw NoEncontrado();
            Entorno(p);
            if (p.FechaCaptura.HasValue || p.OperacionHasta > Ahora) return p;
            if (p.OrdenId is null || p.Estado == "fallido") throw Conflicto("orden_no_disponible", "Primero cree una orden PayPal válida.");
            if (!p.CapturaIniciada) await ExigirComprable(r, v, ct);
            p.BloqueaReserva = true;
            p.Estado = "verificando";
            p.OperacionToken = token;
            p.OperacionHasta = Ahora.AddSeconds(90);
            p.FechaActualizacion = Ahora;
            return p;
        }, ct);
        if (pago.OperacionToken != token) return await LeerAsync(usuario, id, ct);
        try
        {
            var orden = await paypal.ConsultarAsync(pago.OrdenId!, ct);
            ValidarOrden(pago, orden, true);
            if (orden.Estado == "APPROVED" && orden.CapturaId is null && !pago.CapturaIniciada)
            {
                // Registrar intención durable ANTES de enviar dinero. Cualquier resultado incierto queda protegido.
                pago = await Bloqueo(usuario, id, async (r, v) =>
                {
                    var p = await store.PorReservaAsync(id, ct) ?? throw NoEncontrado();
                    if (p.OperacionToken != token || p.FechaCaptura.HasValue) return p;
                    await ExigirComprable(r, v, ct);
                    p.CapturaIniciada = true;
                    p.Estado = "capturando";
                    p.FechaActualizacion = Ahora;
                    return p;
                }, ct);
                if (pago.OperacionToken != token || pago.FechaCaptura.HasValue) return await LeerAsync(usuario, id, ct);
                // El cliente consulta luego la representación completa con importe y destinatario.
                orden = await paypal.CapturarAsync(pago, ct);
            }
            await AplicarAsync(usuario, id, orden, token, null, ct);
        }
        catch
        {
            // El estado durable previo ya evita liberar asientos, incluso si falla este guardado.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await MarcarRevisionAsync(usuario, id, token, cleanup.Token); } catch { /* Se mantiene la protección durable. */ }
            throw;
        }
        return await LeerAsync(usuario, id, ct);
    }

    public async Task<PagoPayPalResponse> ConsultarAsync(int usuario, int id, CancellationToken ct)
    {
        var pago = await store.PorReservaAsync(id, ct)
            ?? throw NoEncontrado();
        if (pago.Reserva.UsuarioId != usuario) throw NoEncontrado();
        if (pago.BloqueaReserva && pago.OrdenId is not null && (pago.OperacionHasta == null || pago.OperacionHasta <= Ahora))
            await ConciliarAsync(pago.PagoId, ct);
        return await LeerAsync(usuario, id, ct);
    }

    public async Task ConciliarAsync(Guid pagoId, CancellationToken ct)
    {
        Config.Validar();
        var p = await store.PorIdAsync(pagoId, ct)
            ?? throw NoEncontrado();
        Entorno(p);
        if (p.OrdenId is null || p.OperacionHasta > Ahora) return;
        var orden = await paypal.ConsultarAsync(p.OrdenId, ct);
        await AplicarAsync(p.Reserva.UsuarioId, p.ReservaId, orden, null, null, ct);
    }

    public async Task ProcesarEventoAsync(System.Text.Json.JsonElement evento, IHeaderDictionary headers, CancellationToken ct)
    {
        Config.ValidarWebhook();
        if (!await paypal.VerificarEventoAsync(evento, headers, ct))
            throw new ReservaOperacionException(ErrorReserva.SolicitudInvalida, "webhook_no_verificado", "Firma de PayPal inválida.");
        var eventId = PayPalClient.Texto(evento, "id");
        var tipo = PayPalClient.Texto(evento, "event_type");
        if (eventId.Length is < 1 or > 100 || tipo.Length is < 1 or > 100)
            throw new ReservaOperacionException(ErrorReserva.SolicitudInvalida, "webhook_invalido", "Evento inválido.");
        if (await store.EventoExisteAsync(eventId, ct)) return;
        var orderId = "";
        var captureId = "";
        if (evento.TryGetProperty("resource", out var resource))
        {
            if (tipo.StartsWith("CHECKOUT.ORDER.", StringComparison.Ordinal)) orderId = PayPalClient.Texto(resource, "id");
            if (resource.TryGetProperty("supplementary_data", out var supplementary) && supplementary.TryGetProperty("related_ids", out var ids))
            { orderId = PayPalClient.Texto(ids, "order_id"); captureId = PayPalClient.Texto(ids, "capture_id"); }
            if (tipo.StartsWith("PAYMENT.CAPTURE.", StringComparison.Ordinal) && captureId == "") captureId = PayPalClient.Texto(resource, "id");
        }
        var pago = await store.PorIdentificadorExternoAsync(orderId, captureId, Config.Entorno, ct);
        if (pago is not null && pago.OrdenId is not null)
        {
            // El evento firmado es aviso; la API oficial confirma el estado y los importes.
            var orden = await paypal.ConsultarAsync(pago.OrdenId, ct);
            await AplicarAsync(pago.Reserva.UsuarioId, pago.ReservaId, orden, null, tipo, ct);
        }
        await store.RegistrarEventoAsync(new EventoPayPal { EventoId = eventId, Tipo = tipo, FechaProcesado = Ahora }, ct);
    }
    private Task<PagoPayPal> AplicarAsync(int usuario, int id, OrdenPayPal orden, Guid? token, string? evento, CancellationToken ct) =>
        Bloqueo(usuario, id, async (r, v) =>
        {
            var p = await store.PorReservaAsync(id, ct) ?? throw NoEncontrado();
            Entorno(p);
            if (token.HasValue && p.OperacionToken != token) return p;
            ValidarOrden(p, orden, true);
            var adverso = evento is "PAYMENT.CAPTURE.REFUNDED" or "PAYMENT.CAPTURE.REVERSED";
            var revisionPrevia = p.Estado == "revision" && p.FechaCaptura.HasValue;
            // Una consulta iniciada antes de otra captura no puede degradar un cobro ya confirmado.
            if (p.FechaCaptura.HasValue && orden.EstadoCaptura is not ("COMPLETED" or "REFUNDED" or "PARTIALLY_REFUNDED") && !adverso) return p;
            if (orden.EstadoCaptura == "COMPLETED")
            {
                if (p.CapturaId is not null && p.CapturaId != orden.CapturaId) throw Conflicto("captura_inconsistente", "El pago requiere revisión.");
                p.CapturaId = orden.CapturaId;
                p.FechaCaptura ??= orden.FechaCaptura ?? Ahora;
                // No resucitar una reserva cancelada ni reasignar un asiento que ya se liberó.
                if (!adverso && !revisionPrevia && p.Monto == r.MontoTotal && r.Estado == "pendiente_pago" && await AsignacionValida(r, v, ct) && VueloAbierto(v))
                {
                    r.Estado = "pagada";
                    reservas.AgregarHistorial(new HistorialCambioReserva { ReservaId = id, TipoAccion = "PAGO_PAYPAL",
                        FechaAccion = Ahora, Observacion = "Captura PayPal confirmada (" + p.Entorno + ")." });
                }
                p.Estado = r.Estado == "pagada" && !adverso && !revisionPrevia ? "completado" : "revision";
                p.BloqueaReserva = p.Estado == "revision" && r.Estado == "pendiente_pago";
            }
            else if (p.FechaCaptura.HasValue || adverso || orden.EstadoCaptura is "REFUNDED" or "PARTIALLY_REFUNDED")
            {
                if ((orden.EstadoCaptura is "REFUNDED" or "PARTIALLY_REFUNDED") && orden.CapturaId is not null)
                {
                    p.CapturaId ??= orden.CapturaId;
                    p.FechaCaptura ??= orden.FechaCaptura ?? Ahora;
                }
                p.Estado = "revision"; p.BloqueaReserva = r.Estado == "pendiente_pago";
            }
            else if (orden.EstadoCaptura is "DENIED" or "DECLINED" or "FAILED" || orden.Estado == "VOIDED")
            { p.Estado = "fallido"; p.BloqueaReserva = false; }
            else if (orden.EstadoCaptura == "PENDING")
            { p.Estado = "pendiente"; p.BloqueaReserva = true; }
            else if (p.CapturaIniciada)
            { p.Estado = "revision"; p.BloqueaReserva = true; }
            else { p.Estado = "creada"; p.BloqueaReserva = false; }
            LiberarOperacion(p);
            return p;
        }, ct);

    private Task<PagoPayPal> MarcarRevisionAsync(int usuario, int id, Guid token, CancellationToken ct) =>
        Bloqueo(usuario, id, async (r, v) =>
        {
            var p = await store.PorReservaAsync(id, ct) ?? throw NoEncontrado();
            if (p.OperacionToken == token)
            {
                p.Estado = p.CapturaIniciada ? "revision" : "creada";
                p.BloqueaReserva = p.CapturaIniciada;
                LiberarOperacion(p);
            }
            return p;
        }, ct);

    private async Task<PagoPayPal> Bloqueo(int usuario, int id, Func<Reserva, Vuelo, Task<PagoPayPal>> accion, CancellationToken ct)
    {
        store.LimpiarSeguimiento();
        try
        {
            var vueloId = await reservas.ObtenerVueloPropioAsync(id, usuario, ct) ?? throw NoEncontrado();
            await using var tx = await reservas.IniciarTransaccionAsync(ct);
            var vuelo = await reservas.BloquearVueloAsync(vueloId, ct) ?? throw NoEncontrado();
            var reserva = await reservas.BloquearPropiaAsync(id, usuario, ct) ?? throw NoEncontrado();
            var pago = await accion(reserva, vuelo);
            await reservas.GuardarAsync(ct);
            await tx.ConfirmarAsync(ct);
            return pago;
        }
        finally { store.LimpiarSeguimiento(); }
    }

    private async Task ExigirComprable(Reserva r, Vuelo v, CancellationToken ct)
    {
        if (r.Estado != "pendiente_pago" || r.FechaExpiracion is null || r.FechaExpiracion <= Ahora ||
            !VueloAbierto(v) || r.MontoTotal <= 0 || !await AsignacionValida(r, v, ct))
            throw Conflicto("reserva_no_comprable", "La reserva venció, cambió o ya no permite el pago.");
    }

    private async Task<bool> AsignacionValida(Reserva r, Vuelo v, CancellationToken ct)
    {
        var detalles = await reservas.ObtenerActivosAsync(r.ReservaId, ct);
        if (detalles.Count == 0 || !ImportesReserva.EsConsistente(r, detalles.Sum(d => d.PrecioCongelado))) return false;
        var asientos = await reservas.ObtenerAsientosAsync(v.VueloId, detalles.Select(d => d.AsientoVueloId).ToArray(), ct);
        return asientos.Count == detalles.Count && asientos.All(a => a.Estado == "reservado");
    }

    public static void ValidarOrden(PagoPayPal pago, OrdenPayPal orden, bool exigirId)
    {
        if (string.IsNullOrWhiteSpace(orden.Id) || orden.Id.Length > 64 || !orden.Id.All(char.IsAsciiLetterOrDigit) ||
            exigirId && pago.OrdenId != orden.Id || orden.CustomId != pago.PagoId.ToString("N") ||
            orden.Monto != pago.Monto || orden.Moneda != "USD" || orden.MerchantId != pago.MerchantId ||
            pago.CapturaId is not null && orden.CapturaId is not null && pago.CapturaId != orden.CapturaId ||
            orden.CapturaId is not null && (orden.CapturaId.Length is < 1 or > 64 ||
                orden.MontoCaptura != pago.Monto || orden.MonedaCaptura != "USD"))
            throw Conflicto("pago_inconsistente", "La identidad, destinatario o importe del pago requiere revisión.");
    }

    private bool VueloAbierto(Vuelo v) => (v.Estado is "programado" or "retrasado") && v.SalidaProgramada > Ahora && !v.SalidaReal.HasValue && !v.LlegadaReal.HasValue;
    private void Entorno(PagoPayPal p)
    { if (p.Entorno != Config.Entorno || p.MerchantId != Config.MerchantId) throw Conflicto("entorno_paypal_distinto", "El pago pertenece a otra configuración de PayPal."); }
    private void LiberarOperacion(PagoPayPal p)
    { p.OperacionHasta = null; p.OperacionToken = null; p.FechaActualizacion = Ahora; }

    private async Task<PagoPayPalResponse> LeerAsync(int usuario, int id, CancellationToken ct)
    {
        var p = await store.PorReservaAsync(id, ct)
            ?? throw NoEncontrado();
        if (p.Reserva.UsuarioId != usuario) throw NoEncontrado();
        return new(p.PagoId, id, p.Estado, p.Reserva.Estado, p.Monto, p.Moneda, p.Entorno, p.Entorno == "Sandbox",
            p.OrdenId, p.CapturaId, p.Reserva.Estado == "pendiente_pago" && p.Reserva.FechaExpiracion > Ahora && !p.CapturaIniciada ? p.UrlAprobacion : null,
            p.FechaCaptura, p.Estado == "revision", Convert.ToBase64String(p.Reserva.RowVersion),
            p.Reserva.Subtotal, p.Reserva.PorcentajeIva, p.Reserva.MontoIva);
    }

    private static ReservaOperacionException NoEncontrado() => new(ErrorReserva.NoEncontrada, "pago_no_encontrado", "Reserva o pago no encontrado.");
    private static ReservaOperacionException Conflicto(string codigo, string mensaje) => new(ErrorReserva.Conflicto, codigo, mensaje);
}
