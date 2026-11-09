namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Vuelo y versión de programación para la siguiente edición administrativa.</summary>
public sealed record VueloAdminResponse(VueloResponse Vuelo, string Version, int CantidadAsientos);
