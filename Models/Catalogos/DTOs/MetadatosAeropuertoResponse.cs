namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Campos adicionales del CSV. DatosOrigenJson conserva los valores originales de todas las columnas, incluidas las base, sin inventar metadatos de registros antiguos.</summary>
public sealed record MetadatosAeropuertoResponse(long? IdOrigen, string? Identificador, string? Tipo,
    double? Latitud, double? Longitud, double? ElevacionPies, string? Continente, string? RegionIso,
    bool? ServicioRegular, string? CodigoIcao, string? CodigoGps, string? CodigoLocal,
    string? SitioWeb, string? Wikipedia, string? PalabrasClave, string? DatosOrigenJson);
