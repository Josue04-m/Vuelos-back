namespace Vuelos_Backend.Models.Autenticacion;

// Información interna para construir claims; nunca contiene el token ni el hash.
public sealed record SesionAutenticada(int SesionId, int UsuarioId, string Rol);
