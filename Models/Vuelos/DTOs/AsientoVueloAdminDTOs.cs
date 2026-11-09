using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Models.Reservas.DTOs;

namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Información administrativa de un asiento asignado a un vuelo.</summary>
public sealed record AsientoVueloAdminResponse(
    int AsientoVueloId,
    int AsientoFisicoId,
    string CodigoAsiento,
    string Categoria,
    decimal Precio,
    string Estado,
    string RowVersion,
    bool TieneReservaActiva);

/// <summary>Actualización de precio o bloqueo de un asiento por vuelo.</summary>
public sealed record ActualizarAsientoVueloAdminRequest : IValidatableObject
{
    /// <summary>Precio base en USD para futuras reservas del asiento en este vuelo.</summary>
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    public decimal? Precio { get; init; }

    /// <summary>Disponibilidad operativa: disponible o bloqueado.</summary>
    [RegularExpression("^(disponible|bloqueado)$", ErrorMessage = "El estado solo puede cambiarse a disponible o bloqueado.")]
    public string? Estado { get; init; }

    /// <summary>RowVersion del asiento en Base64 para control de concurrencia optimista.</summary>
    [Required, VersionFila]
    public required string RowVersion { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Precio is null && Estado is null)
            yield return new ValidationResult("Debe indicar al menos un cambio (Precio o Estado).", [nameof(Precio), nameof(Estado)]);

        if (Precio.HasValue && decimal.Round(Precio.Value, 2) != Precio.Value)
            yield return new ValidationResult("El precio admite como máximo dos decimales.", [nameof(Precio)]);
    }
}
