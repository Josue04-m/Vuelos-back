namespace Vuelos_Backend.Models.Pagos;

public sealed class OpcionesPayPal
{
    public bool Habilitado { get; set; }
    public string Entorno { get; set; } = "Sandbox";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string WebhookId { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";

    public void Validar()
    {
        if (!Habilitado || Entorno is not ("Sandbox") ||
            new[] { ClientId, ClientSecret, MerchantId }.Any(string.IsNullOrWhiteSpace) ||
            MerchantId.Length > 64 || !MerchantId.All(char.IsAsciiLetterOrDigit) ||
            !UrlValida(ReturnUrl) || !UrlValida(CancelUrl))
            throw new Reservas.ReservaOperacionException(Reservas.ErrorReserva.ServicioNoDisponible,
                "paypal_sin_configurar", "Configure PayPal y sus credenciales en secretos de usuario o variables de entorno.");
    }

    public void ValidarWebhook()
    {
        Validar();
        if (string.IsNullOrWhiteSpace(WebhookId))
            throw new Reservas.ReservaOperacionException(Reservas.ErrorReserva.ServicioNoDisponible,
                "paypal_webhook_sin_configurar", "Configure PayPal:WebhookId para verificar notificaciones de PayPal.");
    }

    private bool UrlValida(string valor) => Uri.TryCreate(valor, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.UserInfo) && (uri.Scheme == "https" ||
        Entorno == "Sandbox" && uri.Scheme == "http" && (uri.IsLoopback || EsIpPrivada(uri)));

    // Solo Sandbox: permite pruebas por red privada (LAN/ZeroTier). Live exige HTTPS.
    private static bool EsIpPrivada(Uri uri)
    {
        if (uri.HostNameType != UriHostNameType.IPv4 || !System.Net.IPAddress.TryParse(uri.Host, out var ip)) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168;
    }
}
