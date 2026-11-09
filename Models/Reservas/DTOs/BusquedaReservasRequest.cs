using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Reservas.DTOs;

/// <summary>Filtros de reservas propias por estado y fecha de creación UTC.</summary>
public sealed record BusquedaReservasRequest : IValidatableObject
{
    [RegularExpression("^(confirmada|cancelada|modificada|pendiente_pago|pagada|expirada)$")]
    public string? Estado { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaDesde.HasValue != FechaHasta.HasValue || FechaDesde > FechaHasta)
            yield return new ValidationResult("Use un rango completo y ordenado de fechas UTC.", [nameof(FechaDesde), nameof(FechaHasta)]);
        if ((long)(Pagina - 1) * TamanoPagina > int.MaxValue)
            yield return new ValidationResult("La página supera el desplazamiento permitido.", [nameof(Pagina)]);
    }
}
