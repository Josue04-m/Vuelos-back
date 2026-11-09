using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Autenticacion.DTOs;

/// <summary>Credenciales para iniciar una sesión.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(150)]
    public required string Email { get; init; }
    [Required, StringLength(128)]
    public required string Password { get; init; }
}
