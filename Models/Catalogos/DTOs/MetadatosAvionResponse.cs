namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Configuración y códigos del avión importado. JSON conserva los nueve valores originales del CSV; null indica datos no cargados, no cero.</summary>
public sealed record MetadatosAvionResponse(string? CodigoIata, string? CodigoIcao, int? CantidadPrimeraClase,
    int? CantidadSegundaClase, int? CantidadTerceraClase, string? LetrasPorFila, string? DatosOrigenJson);
