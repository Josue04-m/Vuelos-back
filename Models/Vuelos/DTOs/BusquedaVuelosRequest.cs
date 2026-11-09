using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Filtros opcionales de salida UTC. Usar fecha o un rango completo, sin combinarlos.</summary>
public sealed record BusquedaVuelosRequest : IValidatableObject
{
    /// <summary>Código IATA de origen, por ejemplo GYE; no distingue mayúsculas.</summary>
    [RegularExpression("^[A-Za-z]{3}$")]
    public string? Origen { get; init; }
    /// <summary>Código IATA de destino, por ejemplo UIO.</summary>
    [RegularExpression("^[A-Za-z]{3}$")]
    public string? Destino { get; init; }
    /// <summary>Día UTC de salida, formato YYYY-MM-DD.</summary>
    public DateOnly? Fecha { get; init; }
    /// <summary>Primer día UTC del rango, incluido.</summary>
    public DateOnly? FechaDesde { get; init; }
    /// <summary>Último día UTC del rango, incluido.</summary>
    public DateOnly? FechaHasta { get; init; }
    /// <summary>Página desde 1, ordenada por salida e identificador.</summary>
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;
    /// <summary>Elementos por página: entre 1 y 100, por defecto 20.</summary>
    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Origen is not null && Destino is not null &&
            string.Equals(Origen, Destino, StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("Origen y destino deben ser diferentes.", [nameof(Origen), nameof(Destino)]);
        if (Fecha.HasValue && (FechaDesde.HasValue || FechaHasta.HasValue))
            yield return new ValidationResult("Use fecha o el rango fechaDesde/fechaHasta, sin combinarlos.",
                [nameof(Fecha), nameof(FechaDesde), nameof(FechaHasta)]);
        if (FechaDesde.HasValue != FechaHasta.HasValue)
            yield return new ValidationResult("El rango necesita fechaDesde y fechaHasta.",
                [nameof(FechaDesde), nameof(FechaHasta)]);
        if (FechaDesde > FechaHasta)
            yield return new ValidationResult("fechaHasta debe ser igual o posterior a fechaDesde.",
                [nameof(FechaHasta)]);
        if ((long)(Pagina - 1) * TamanoPagina > int.MaxValue)
            yield return new ValidationResult("La página solicitada supera el desplazamiento permitido.",
                [nameof(Pagina)]);
    }
}
