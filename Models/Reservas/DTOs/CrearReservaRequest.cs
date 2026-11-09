using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Reservas.DTOs;

/// <summary>Pasajero con asiento propio, incluidos bebés. No se solicita fecha de nacimiento.</summary>
public sealed record PasajeroReservaRequest
{
    [Required, StringLength(50)] public required string Cedula { get; init; }
    [Required, StringLength(100)] public required string PrimerNombre { get; init; }
    [StringLength(100)] public string? SegundoNombre { get; init; }
    [Required, StringLength(100)] public required string PrimerApellido { get; init; }
    [StringLength(100)] public string? SegundoApellido { get; init; }
    [Required, RegularExpression("^(adulto|joven|nino|bebe)$")]
    public required string TipoPasajero { get; init; }
    [Range(1, int.MaxValue)] public int AsientoVueloId { get; init; }
    [Required, VersionFila] public required string RowVersionAsiento { get; init; }

}

/// <summary>Reserva de un vuelo, realizada por el usuario de la sesión, viaje o no ese usuario.</summary>
public sealed record CrearReservaRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int VueloId { get; init; }
    /// <summary>Total final en USD, incluido el IVA del laboratorio; no el subtotal de los pasajeros.</summary>
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public required decimal TotalEsperado { get; init; }
    [Required, MinLength(1)] public required List<PasajeroReservaRequest> Pasajeros { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TotalEsperado != decimal.Round(TotalEsperado, 2))
            yield return new ValidationResult("El total debe tener como máximo dos decimales.", [nameof(TotalEsperado)]);
        if (Pasajeros is null) yield break;
        if (Pasajeros.Any(p => p is null))
        {
            yield return new ValidationResult("Los pasajeros no pueden ser null.", [nameof(Pasajeros)]);
            yield break;
        }
        if (Pasajeros.Select(p => p.AsientoVueloId).Distinct().Count() != Pasajeros.Count)
            yield return new ValidationResult("No se puede repetir un asiento.", [nameof(Pasajeros)]);
        if (Pasajeros.Select(p => p.Cedula?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Pasajeros.Count)
            yield return new ValidationResult("No se puede repetir la cédula dentro de la reserva.", [nameof(Pasajeros)]);
    }
}
