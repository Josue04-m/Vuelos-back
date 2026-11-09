using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Usuarios.DTOs;

/// <summary>Datos para registrar un cliente. El rol lo asigna el servidor.</summary>
public sealed record RegistroUsuarioRequest
{
    [Required, StringLength(100)]
    public required string Nombre { get; init; }
    [Required, StringLength(100)]
    public required string Apellido { get; init; }
    [Required, EmailAddress, StringLength(150)]
    public required string Email { get; init; }
    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; init; }
}
