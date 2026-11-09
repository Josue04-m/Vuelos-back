using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Pagos;
using Vuelos_Backend.Models.Pagos;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Services.Pagos;

/// <summary>REST oficial Orders v2. Nunca recibe importes ni credenciales del navegador.</summary>
public sealed class PayPalClient(HttpClient http, IOptions<OpcionesPayPal> opciones) : IPayPalClient
{
    private readonly OpcionesPayPal config = opciones.Value;
    private Uri Base => new(config.Entorno == "Sandbox" ? "https://api-m.sandbox.paypal.com/" : "https://api-m.paypal.com/");

    public async Task<OrdenPayPal> CrearAsync(PagoPayPal pago, CancellationToken ct)
    {
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[] { new { reference_id = pago.PagoId.ToString("N"), custom_id = pago.PagoId.ToString("N"),
                invoice_id = pago.PagoId.ToString("N"), payee = new { merchant_id = pago.MerchantId },
                amount = new { currency_code = "USD", value = pago.Monto.ToString("0.00", CultureInfo.InvariantCulture) } } },
            payment_source = new { paypal = new { experience_context = new { return_url = config.ReturnUrl,
                cancel_url = config.CancelUrl, user_action = "PAY_NOW", shipping_preference = "NO_SHIPPING" } } }
        };
        using var json = await EnviarAsync(HttpMethod.Post, "v2/checkout/orders", body, "crear-" + pago.PagoId.ToString("N"), ct);
        return LeerOrden(json.RootElement);
    }

    public async Task<OrdenPayPal> ConsultarAsync(string ordenId, CancellationToken ct)
    {
        if (ordenId.Length is < 1 or > 64 || !ordenId.All(char.IsAsciiLetterOrDigit)) throw Proveedor();
        using var json = await EnviarAsync(HttpMethod.Get, "v2/checkout/orders/" + ordenId, null, null, ct);
        return LeerOrden(json.RootElement);
    }

    public async Task<OrdenPayPal> CapturarAsync(PagoPayPal pago, CancellationToken ct)
    {
        // Idempotencia persistente y distinta de la creación; nunca cambia al reintentar.
        using var json = await EnviarAsync(HttpMethod.Post, "v2/checkout/orders/" + pago.OrdenId + "/capture",
            new { }, "cobro-" + pago.PagoId.ToString("N"), ct);
        return await ConsultarAsync(pago.OrdenId!, ct);
    }

    public async Task<bool> VerificarEventoAsync(JsonElement evento, IHeaderDictionary headers, CancellationToken ct)
    {
        string Header(string nombre)
        {
            var valor = headers[nombre].ToString();
            if (string.IsNullOrWhiteSpace(valor) || valor.Length > 4096) throw new ReservaOperacionException(
                ErrorReserva.SolicitudInvalida, "webhook_invalido", "Faltan encabezados válidos de PayPal.");
            return valor;
        }
        var cert = Header("PAYPAL-CERT-URL");
        if (!Uri.TryCreate(cert, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Port != 443 || uri.UserInfo != "" ||
            !(uri.Host == "paypal.com" || uri.Host.EndsWith(".paypal.com", StringComparison.OrdinalIgnoreCase)))
            return false;
        using var json = await EnviarAsync(HttpMethod.Post, "v1/notifications/verify-webhook-signature", new
        {
            auth_algo = Header("PAYPAL-AUTH-ALGO"), cert_url = cert,
            transmission_id = Header("PAYPAL-TRANSMISSION-ID"), transmission_sig = Header("PAYPAL-TRANSMISSION-SIG"),
            transmission_time = Header("PAYPAL-TRANSMISSION-TIME"), webhook_id = config.WebhookId, webhook_event = evento
        }, null, ct);
        return Texto(json.RootElement, "verification_status") == "SUCCESS";
    }

    private async Task<JsonDocument> EnviarAsync(HttpMethod metodo, string ruta, object? body, string? requestId, CancellationToken ct)
    {
        config.Validar();
        try
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(Base, "v1/oauth2/token"));
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(config.ClientId + ":" + config.ClientSecret)));
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
            using var tokenResponse = await http.SendAsync(tokenRequest, ct);
            if (!tokenResponse.IsSuccessStatusCode) throw Proveedor();
            using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct));
            var token = Texto(tokenJson.RootElement, "access_token");
            if (string.IsNullOrWhiteSpace(token)) throw Proveedor();
            using var request = new HttpRequestMessage(metodo, new Uri(Base, ruta));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
            if (requestId is not null) request.Headers.Add("PayPal-Request-Id", requestId);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw Proveedor();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or FormatException)
        {
            // El llamador conserva la protección de la reserva ante un resultado incierto de captura.
            throw Proveedor();
        }
    }

    public static OrdenPayPal LeerOrden(JsonElement root)
    {
        try
        {
            var units = root.GetProperty("purchase_units");
            if (units.GetArrayLength() != 1) throw Proveedor();
            var unit = units[0];
            var amount = unit.GetProperty("amount");
            JsonElement? capture = null;
            if (unit.TryGetProperty("payments", out var payments) && payments.TryGetProperty("captures", out var captures))
            {
                if (captures.GetArrayLength() > 1) throw Proveedor();
                if (captures.GetArrayLength() == 1) capture = captures[0];
            }
            string? link = null;
            if (root.TryGetProperty("links", out var links))
                foreach (var l in links.EnumerateArray())
                    if (Texto(l, "rel") is "approve" or "payer-action")
                    {
                        var href = Texto(l, "href");
                        if (Uri.TryCreate(href, UriKind.Absolute, out var url) && url.Scheme == "https" &&
                            (url.Host == "paypal.com" || url.Host.EndsWith(".paypal.com", StringComparison.OrdinalIgnoreCase))) link = href;
                    }
            var merchant = unit.TryGetProperty("payee", out var payee) ? Texto(payee, "merchant_id") : "";
            return new(Texto(root, "id"), Texto(root, "status"), link, Texto(unit, "custom_id"),
                Decimal(amount), Texto(amount, "currency_code"), merchant,
                capture.HasValue ? Texto(capture.Value, "id") : null,
                capture.HasValue ? Texto(capture.Value, "status") : null,
                capture.HasValue ? Decimal(capture.Value.GetProperty("amount")) : null,
                capture.HasValue ? Texto(capture.Value.GetProperty("amount"), "currency_code") : null,
                capture.HasValue && capture.Value.TryGetProperty("create_time", out var date) ? date.GetDateTime().ToUniversalTime() : null);
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException)
        { throw Proveedor(); }
    }

    public static string Texto(JsonElement value, string name) => value.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()! : "";
    private static decimal Decimal(JsonElement amount) => decimal.Parse(Texto(amount, "value"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    private static ReservaOperacionException Proveedor() => new(ErrorReserva.ServicioNoDisponible,
        "paypal_no_disponible", "No se pudo confirmar la respuesta de PayPal. Consulte el estado del pago antes de reintentar.");
}
