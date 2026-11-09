namespace Vuelos_Backend.Models.Common;

public interface ICatalogoAdministrable
{
    bool Activo { get; set; }
    DateTime? FechaDesactivacion { get; set; }
    byte[] RowVersion { get; set; }
}
