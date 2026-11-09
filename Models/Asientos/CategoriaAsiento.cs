namespace Vuelos_Backend.Models.Asientos;

public class CategoriaAsiento : Vuelos_Backend.Models.Common.ICatalogoAdministrable
{
    public bool Activo { get; set; } = true;
    public DateTime? FechaDesactivacion { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int CategoriaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
}

