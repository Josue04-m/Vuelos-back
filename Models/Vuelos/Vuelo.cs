using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Aviones;

namespace Vuelos_Backend.Models.Vuelos;

public class Vuelo
{
    public int VueloId { get; set; }
    public string NumeroVuelo { get; set; } = null!;
    public int AvionId { get; set; }
    public int AeropuertoOrigenId { get; set; }
    public int AeropuertoDestinoId { get; set; }
    public DateTime SalidaProgramada { get; set; }
    public DateTime LlegadaProgramada { get; set; }
    public DateTime? SalidaReal { get; set; }
    public DateTime? LlegadaReal { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }

    public Avion Avion { get; set; } = null!;

    public Aeropuerto AeropuertoOrigen { get; set; } = null!;

    public Aeropuerto AeropuertoDestino { get; set; } = null!;
}

