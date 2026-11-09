namespace Vuelos_Backend.Models.Usuarios.DTOs;

/// <summary>Información pública del cliente registrado.</summary>
public sealed record UsuarioResponse(int UsuarioId, string Nombre, string Apellido, string Email);
