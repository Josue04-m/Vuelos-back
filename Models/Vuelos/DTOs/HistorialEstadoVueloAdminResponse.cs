namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Registro histórico de cambio de estado operativo del vuelo.</summary>
public sealed record HistorialEstadoVueloAdminResponse(
    int HistorialId,
    int VueloId,
    string? EstadoAnterior,
    string EstadoNuevo,
    DateTimeOffset FechaCambio,
    int UsuarioAdminId,
    string? AdminNombre,
    string? Observacion);
