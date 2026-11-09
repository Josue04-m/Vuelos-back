namespace Vuelos_Backend.Models.Aviones;

public class Avion : Vuelos_Backend.Models.Common.ICatalogoAdministrable
{
    public bool Activo { get; set; } = true;
    public DateTime? FechaDesactivacion { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int AvionId { get; set; }
    public string Modelo { get; set; } = null!;
    public string Matricula { get; set; } = null!;
    public int CapacidadTotal { get; set; }
    public string? CodigoIata { get; set; }
    public string? CodigoIcao { get; set; }
    public int? CantidadPrimeraClase { get; set; }
    public int? CantidadSegundaClase { get; set; }
    public int? CantidadTerceraClase { get; set; }
    public string? LetrasPorFila { get; set; }
    public string? DatosOrigenJson { get; set; }
}

