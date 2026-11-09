namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Detalle de pasajero en una reserva de vuelo para administración.</summary>
public sealed record PasajeroVueloAdminResponse(
    int DetalleId,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    string DocumentoIdentidad,
    string? TipoPasajero,
    string CodigoAsiento,
    decimal PrecioCongelado,
    string Estado);

/// <summary>Reserva de un vuelo consultada desde la administración.</summary>
public sealed record ReservaVueloAdminResponse(
    int ReservaId,
    string CodigoReserva,
    string Estado,
    decimal MontoTotal,
    DateTimeOffset FechaReserva,
    int UsuarioId,
    string TitularNombre,
    string TitularEmail,
    IReadOnlyList<PasajeroVueloAdminResponse> Pasajeros,
    decimal? Subtotal = null, decimal? PorcentajeIva = null, decimal? MontoIva = null);
