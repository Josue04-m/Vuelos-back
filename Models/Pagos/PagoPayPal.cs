using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Models.Pagos;

public sealed class PagoPayPal
{
    public Guid PagoId { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public string Entorno { get; set; } = "Sandbox";
    public string Estado { get; set; } = "creando";
    public string? OrdenId { get; set; }
    public string? CapturaId { get; set; }
    public string? UrlAprobacion { get; set; }
    public string MerchantId { get; set; } = "";
    public bool BloqueaReserva { get; set; }
    public bool CapturaIniciada { get; set; }
    public Guid? OperacionToken { get; set; }
    public DateTime? OperacionHasta { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class EventoPayPal
{
    public string EventoId { get; set; } = "";
    public string Tipo { get; set; } = "";
    public DateTime FechaProcesado { get; set; }
}
