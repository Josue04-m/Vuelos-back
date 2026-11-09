using Vuelos_Backend.Models.Asientos;

namespace Vuelos_Backend.Models.Reservas;

public class HistorialCambioReserva
{
    public int HistorialReservaId { get; set; }
    public int ReservaId { get; set; }
    public int? AsientoVueloAnteriorId { get; set; }
    public int? AsientoVueloNuevoId { get; set; }
    public string TipoAccion { get; set; } = null!;
    public DateTime FechaAccion { get; set; }
    public string? Observacion { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public AsientoVuelo? AsientoVueloAnterior { get; set; }
    public AsientoVuelo? AsientoVueloNuevo { get; set; }
}

