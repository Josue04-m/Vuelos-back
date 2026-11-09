namespace Vuelos_Backend.Models.Aeropuertos;

public class Aeropuerto : Vuelos_Backend.Models.Common.ICatalogoAdministrable
{
    public bool Activo { get; set; } = true;
    public DateTime? FechaDesactivacion { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int AeropuertoId { get; set; }
    public string CodigoIata { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Ciudad { get; set; } = null!;
    public string Pais { get; set; } = null!;
    public long? IdOrigen { get; set; }
    public string? Identificador { get; set; }
    public string? Tipo { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public double? ElevacionPies { get; set; }
    public string? Continente { get; set; }
    public string? RegionIso { get; set; }
    public bool? ServicioRegular { get; set; }
    public string? CodigoIcao { get; set; }
    public string? CodigoGps { get; set; }
    public string? CodigoLocal { get; set; }
    public string? SitioWeb { get; set; }
    public string? Wikipedia { get; set; }
    public string? PalabrasClave { get; set; }
    public string? DatosOrigenJson { get; set; }
}

