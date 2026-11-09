using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Solicitud administrativa para cambiar el estado operativo de un vuelo.</summary>
public sealed record CambiarEstadoVueloAdminRequest
{
    /// <summary>Nuevo estado operativo: programado, embarcando, retrasado, en vuelo, finalizado o cancelado.</summary>
    [Required, RegularExpression("^(programado|embarcando|retrasado|en vuelo|finalizado|cancelado)$",
        ErrorMessage = "El estado debe ser: programado, embarcando, retrasado, en vuelo, finalizado o cancelado.")]
    public required string NuevoEstado { get; init; }

    /// <summary>Versión del vuelo obtenida del último GET administrativo para evitar colisiones.</summary>
    [Required, RegularExpression("^[A-Fa-f0-9]{64}$", ErrorMessage = "La versión debe ser un hash hexadecimal de 64 caracteres.")]
    public required string Version { get; init; }

    /// <summary>Observación opcional para registrar en el historial de estados.</summary>
    [StringLength(255, ErrorMessage = "La observación admite como máximo 255 caracteres.")]
    public string? Observacion { get; init; }

    /// <summary>Solo al pasar a en vuelo. No futura, con zona explícita. Si se omite se usa la hora UTC actual.</summary>
    [JsonConverter(typeof(FechaConZonaConverter))]
    public DateTimeOffset? SalidaReal { get; init; }

    /// <summary>Solo al finalizar. No futura y posterior a la salida, con zona explícita. Por defecto hora UTC actual.</summary>
    [JsonConverter(typeof(FechaConZonaConverter))]
    public DateTimeOffset? LlegadaReal { get; init; }
}
