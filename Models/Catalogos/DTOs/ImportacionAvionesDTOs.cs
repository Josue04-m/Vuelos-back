using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>CSV UTF-8 separado por punto y coma y categorías activas elegidas por el administrador.</summary>
public sealed record ImportarAvionesRequest
{
    [Required] public IFormFile Archivo { get; init; } = null!;
    [Range(1, int.MaxValue)] public int PrimeraClaseId { get; init; }
    [Range(1, int.MaxValue)] public int SegundaClaseId { get; init; }
    [Range(1, int.MaxValue)] public int TerceraClaseId { get; init; }
}

/// <summary>Error localizado por línea física de inicio del registro; fila 1 corresponde a encabezados/configuración.</summary>
public sealed record ErrorImportacionAvionResponse(int Fila, string Campo, string Mensaje);

/// <summary>Fila normalizada, existente o pendiente de crear. Guarda códigos, distribución y valores originales del CSV.</summary>
public sealed record FilaImportacionAvionResponse(int Fila, string Modelo, string Matricula, int CapacidadTotal,
    int CantidadPrimeraClase, int CantidadSegundaClase, int CantidadTerceraClase, string LetrasPorFila,
    string CodigoIata, string CodigoIcao, bool MatriculaRegistrada, int? AvionId, string? DatosOrigenJson = null);

/// <summary>Vista previa o resultado atómico. Ya importados y conflictos nunca crean registros.</summary>
public sealed record ImportacionAvionesResponse(string Estado, string Mensaje, bool PuedeImportar,
    int TotalFilas, int TotalAsientos, int AvionesImportados, int AsientosImportados,
    IReadOnlyList<FilaImportacionAvionResponse> Filas, IReadOnlyList<ErrorImportacionAvionResponse> Errores,
    IReadOnlyList<string> Advertencias);
