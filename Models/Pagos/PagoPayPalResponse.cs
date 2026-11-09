using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Pagos;

public sealed record CrearOrdenPayPalRequest
{
    [Required, RegularExpression(@"^[A-Za-z0-9+/]{11}=$")]
    public string RowVersion { get; init; } = "";
}

public sealed record PagoPayPalResponse
    (
    Guid PagoId, 
    int ReservaId, 
    string Estado, 
    string EstadoReserva,
    decimal Monto, 
    string Moneda, 
    string Entorno, 
    bool EsSandbox, 
    string? OrdenId, 
    string? CapturaId,
    string? UrlAprobacion, 
    DateTime? FechaCaptura, 
    bool RequiereRevision, 
    string RowVersionReserva,
    decimal? Subtotal = null, 
    decimal? PorcentajeIva = null, 
    decimal? MontoIva = null
    );

public sealed record OrdenPayPal
    (
    string Id, 
    string Estado, 
    string? UrlAprobacion, 
    string CustomId,
    decimal Monto, 
    string Moneda, 
    string MerchantId, 
    string? CapturaId, 
    string? EstadoCaptura,
    decimal? MontoCaptura, 
    string? MonedaCaptura, 
    DateTime? FechaCaptura
    );
