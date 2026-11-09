using Vuelos_Backend.Models.Asientos;

namespace Vuelos_Backend.Models.Reservas;

public class DetalleReserva
{
    public int DetalleId { get; set; }
    public int ReservaId { get; set; }
    public int AsientoVueloId { get; set; }
    public string PrimerNombre { get; set; } = null!;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = null!;
    public string? SegundoApellido { get; set; }
    public string DocumentoIdentidad { get; set; } = null!;
    // Ampliación para tipos de pasajero. NULL identifica registros históricos sin clasificar.
    public string? TipoPasajero { get; set; }
    public decimal PrecioCongelado { get; set; }
    public string Estado { get; set; } = null!;

    public Reserva Reserva { get; set; } = null!;

    public AsientoVuelo AsientoVuelo { get; set; } = null!;
}

