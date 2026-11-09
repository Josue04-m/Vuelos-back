using Vuelos_Backend.Models.Aviones;

namespace Vuelos_Backend.Models.Asientos;

public class AsientoFisico : Vuelos_Backend.Models.Common.ICatalogoAdministrable
{
    public bool Activo { get; set; } = true;
    public DateTime? FechaDesactivacion { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int AsientoFisicoId { get; set; }
    public int AvionId { get; set; }
    public int CategoriaId { get; set; }
    public string CodigoAsiento { get; set; } = null!;

    public Avion Avion { get; set; } = null!;

    public CategoriaAsiento Categoria { get; set; } = null!;
}

