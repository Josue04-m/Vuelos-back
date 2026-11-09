using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Models.Asientos;

public class AsientoVuelo
{
    public int AsientoVueloId { get; set; }
    public int VueloId { get; set; }
    public int AsientoFisicoId { get; set; }
    public decimal Precio { get; set; }
    public string Estado { get; set; } = null!;

    public byte[] RowVersion { get; set; } = null!;

    public Vuelo Vuelo { get; set; } = null!;

    public AsientoFisico AsientoFisico { get; set; } = null!;
}

