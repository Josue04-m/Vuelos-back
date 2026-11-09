using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Creación de un vuelo programado y todos los asientos físicos de su avión, con precios en USD.</summary>
public sealed record CrearVueloAdminRequest
{
    [Required, StringLength(20), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 -]*$")]
    public required string NumeroVuelo { get; init; }
    [Range(1, int.MaxValue)]
    public required int AvionId { get; init; }
    [Range(1, int.MaxValue)]
    public required int AeropuertoOrigenId { get; init; }
    [Range(1, int.MaxValue)]
    public required int AeropuertoDestinoId { get; init; }
    [JsonConverter(typeof(FechaConZonaConverter))]
    public required DateTimeOffset SalidaProgramada { get; init; }
    [JsonConverter(typeof(FechaConZonaConverter))]
    public required DateTimeOffset LlegadaProgramada { get; init; }
    [Required, MinLength(1), MaxLength(1000)]
    public required IReadOnlyList<PrecioClaseAdminRequest> TarifasPorClase { get; init; }
}

/// <summary>Precio base en USD de todos los asientos de una categoría para este vuelo.</summary>
public sealed record PrecioClaseAdminRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public required int CategoriaId { get; init; }
    [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true)]
    public required decimal Precio { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(Precio, 2) != Precio)
            yield return new ValidationResult("El precio admite como máximo dos decimales.", [nameof(Precio)]);
    }
}

/// <summary>Cambio de avión antes de cualquier reserva, con tarifas para las clases del avión nuevo.</summary>
public sealed record CambiarAvionVueloRequest
{
    [Range(1, int.MaxValue)] public required int AvionId { get; init; }
    [Required, RegularExpression("^[A-Fa-f0-9]{64}$")] public required string Version { get; init; }
    [Required, MinLength(1), MaxLength(1000)] public required IReadOnlyList<PrecioClaseAdminRequest> TarifasPorClase { get; init; }
}

/// <summary>Edición completa de la programación. No cambia avión, asientos, precios ni estado.</summary>
public sealed record EditarVueloAdminRequest
{
    [Required, StringLength(20), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 -]*$")]
    public required string NumeroVuelo { get; init; }
    [Range(1, int.MaxValue)]
    public required int AeropuertoOrigenId { get; init; }
    [Range(1, int.MaxValue)]
    public required int AeropuertoDestinoId { get; init; }
    [JsonConverter(typeof(FechaConZonaConverter))]
    public required DateTimeOffset SalidaProgramada { get; init; }
    [JsonConverter(typeof(FechaConZonaConverter))]
    public required DateTimeOffset LlegadaProgramada { get; init; }
    /// <summary>Versión obtenida del último GET administrativo; evita sobrescribir otra edición.</summary>
    [Required, RegularExpression("^[A-Fa-f0-9]{64}$")]
    public required string Version { get; init; }
}

// DateTimeOffset acepta fechas sin zona por defecto. Estos contratos exigen Z o un offset explícito.
public sealed class FechaConZonaConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var texto = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (texto is null || !texto.Contains('T') ||
            !System.Text.RegularExpressions.Regex.IsMatch(texto, @"(Z|[+-]\d{2}:\d{2})$") ||
            !reader.TryGetDateTimeOffset(out var fecha))
            throw new JsonException("La fecha debe incluir hora y zona explícita: Z o ±HH:mm.");
        return fecha;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
