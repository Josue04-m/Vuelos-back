namespace Vuelos_Backend.Models.Reservas;

public sealed class CompraSimulada
{
    public Guid CompraId { get; set; }
    public int ReservaId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaCompra { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public bool EsSimulada { get; set; } = true;
    public Reserva Reserva { get; set; } = null!;
}
