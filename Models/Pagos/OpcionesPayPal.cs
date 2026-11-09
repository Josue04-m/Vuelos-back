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
            new[] { ClientId, ClientSecret, MerchantId, WebhookId }.Any(string.IsNullOrWhiteSpace) ||
            MerchantId.Length > 64 || !MerchantId.All(char.IsAsciiLetterOrDigit) ||
            !UrlValida(ReturnUrl) || !UrlValida(CancelUrl))
            throw new Reservas.ReservaOperacionException(Reservas.ErrorReserva.ServicioNoDisponible,
                "paypal_sin_configurar", "Configure PayPal y sus credenciales en secretos de usuario o variables de entorno.");
    }

    private bool UrlValida(string valor) => Uri.TryCreate(valor, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.UserInfo) && (uri.Scheme == "https" ||
        Entorno == "Sandbox" && uri.IsLoopback && uri.Scheme == "http");
}
