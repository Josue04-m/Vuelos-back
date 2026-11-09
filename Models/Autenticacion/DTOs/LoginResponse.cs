using Vuelos_Backend.Models.Usuarios.DTOs;
namespace Vuelos_Backend.Models.Autenticacion.DTOs;

/// <summary>Token de la nueva sesión y su expiración UTC; solo se devuelve en login.</summary>
public sealed record LoginResponse(string Token, string TipoToken, DateTimeOffset FechaExpiracion, UsuarioResponse Usuario);
