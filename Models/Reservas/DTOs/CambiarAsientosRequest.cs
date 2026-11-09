using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Reservas.DTOs;

/// <summary>Nuevo asiento para un detalle activo; el pasajero y su tipo se conservan.</summary>
public sealed record CambioAsientoRequest
{
    [Range(1, int.MaxValue)] public int DetalleId { get; init; }
    [Range(1, int.MaxValue)] public int AsientoVueloId { get; init; }
    [Required, VersionFila] public required string RowVersionAsiento { get; init; }
}

/// <summary>Cambios de asiento atómicos. Los detalles no incluidos conservan su tarifa congelada.</summary>
public sealed record CambiarAsientosRequest : IValidatableObject
{
    [Required, VersionFila] public required string RowVersion { get; init; }
    /// <summary>Total final incluido IVA si la reserva tiene desglose; las reservas históricas conservan el cálculo anterior.</summary>
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public required decimal TotalEsperado { get; init; }
    [Required, MinLength(1)] public required List<CambioAsientoRequest> Cambios { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TotalEsperado != decimal.Round(TotalEsperado, 2))
            yield return new ValidationResult("El total debe tener como máximo dos decimales.", [nameof(TotalEsperado)]);
        if (Cambios is null) yield break;
        if (Cambios.Any(c => c is null))
        {
            yield return new ValidationResult("Los cambios no pueden ser null.", [nameof(Cambios)]);
            yield break;
        }
        if (Cambios.Select(c => c.DetalleId).Distinct().Count() != Cambios.Count ||
            Cambios.Select(c => c.AsientoVueloId).Distinct().Count() != Cambios.Count)
            yield return new ValidationResult("No se pueden repetir detalles ni asientos destino.", [nameof(Cambios)]);
    }
}

/// <summary>Versión de la reserva que se desea cancelar.</summary>
public sealed record CancelarReservaRequest
{
    [Required, VersionFila] public required string RowVersion { get; init; }
}
