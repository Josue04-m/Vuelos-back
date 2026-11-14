using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Archivo airports.csv con columnas originales, filtros y página de vista previa. La confirmación procesa todos los seleccionados, no solo la página.</summary>
public sealed record ImportarAeropuertosRequest
{
    [Required] public IFormFile Archivo { get; init; } = null!;
    public bool SoloServicioRegular { get; init; } = true;
    [RegularExpression("^[A-Za-z]{2}$")] public string? PaisIso { get; init; }
    public bool OmitirIncompletos { get; init; }
    [Range(1, 150000)] public int Pagina { get; init; } = 1;
    [Range(1, 200)] public int TamanoPagina { get; init; } = 100;
}

/// <summary>Aeropuerto válido del archivo; el país se conserva como código ISO de dos letras.</summary>
public sealed record FilaImportacionAeropuertoResponse(int Fila, string CodigoIata, string Nombre, string? Ciudad,
    string Pais, bool CodigoRegistrado, int? AeropuertoId, MetadatosAeropuertoResponse? Metadatos = null);

/// <summary>Error localizado por línea física inicial del registro. Fila 1 representa encabezados o límites globales.</summary>
public sealed record ErrorImportacionAeropuertoResponse(int Fila, string Campo, string Mensaje);

/// <summary>Resultado global. TotalImportables cuenta nuevos candidatos; TotalExistentes cuenta los omitidos por IATA ya registrado y está incluido en TotalOmitidos. Filas/TotalPaginas incluyen candidatos válidos nuevos y existentes. Hasta 200 errores reales.</summary>
public sealed record ImportacionAeropuertosResponse(string Estado, string Mensaje, bool PuedeImportar,
    int TotalRegistros, int TotalImportables, int TotalOmitidos, int TotalErrores, bool ErroresTruncados,
    int TotalExistentes, int AeropuertosImportados, int Pagina, int TamanoPagina, int TotalPaginas,
    IReadOnlyList<FilaImportacionAeropuertoResponse> Filas, IReadOnlyList<ErrorImportacionAeropuertoResponse> Errores,
    IReadOnlyDictionary<string, int> ResumenOmisiones);
