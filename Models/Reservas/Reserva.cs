using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Models.Reservas;

public class Reserva
{
    public int ReservaId { get; set; }
    public string CodigoReserva { get; set; } = null!;
    public int UsuarioId { get; set; }
    public int VueloId { get; set; }
    public DateTime FechaReserva { get; set; }
    // NULL identifica una reserva anterior al flujo temporal; no implica que esté pagada.
    public DateTime? FechaExpiracion { get; set; }
    public decimal MontoTotal { get; set; }
    // NULL en las tres columnas identifica reservas anteriores al desglose del laboratorio.
    public decimal? Subtotal { get; set; }
    public decimal? PorcentajeIva { get; set; }
    public decimal? MontoIva { get; set; }
    public string Estado { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Vuelo Vuelo { get; set; } = null!;
}

