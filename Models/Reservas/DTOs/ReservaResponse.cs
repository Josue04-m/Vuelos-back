namespace Vuelos_Backend.Models.Reservas.DTOs;

/// <summary>Asignación activa o liberada de un pasajero; los importes son los congelados.</summary>
public sealed record DetalleReservaResponse(
    int DetalleId, int AsientoVueloId, string CodigoAsiento,
    string PrimerNombre, string? SegundoNombre, string PrimerApellido, string? SegundoApellido,
    string Cedula, string? TipoPasajero,
    decimal PrecioCongelado, string Estado, string RowVersionAsiento);

/// <summary>Evento de cambio de asiento o cancelación; no se elimina el historial.</summary>
public sealed record HistorialReservaResponse(
    int HistorialReservaId, string TipoAccion, DateTimeOffset FechaAccion,
    int? AsientoVueloAnteriorId, int? AsientoVueloNuevoId, string? Observacion);

/// <summary>Reserva propia con detalles, historial, total USD y versión actual.</summary>
public sealed record ReservaResponse(
    int ReservaId, string CodigoReserva, int VueloId, string NumeroVuelo,
    DateTimeOffset FechaReserva, decimal MontoTotal, string Moneda, string Estado, string RowVersion,
    IReadOnlyList<DetalleReservaResponse> Detalles, IReadOnlyList<HistorialReservaResponse> Historial,
    DateTimeOffset? FechaExpiracion = null, bool AnteriorAlFlujoTemporal = false, CompraSimuladaResponse? Compra = null,
    decimal? Subtotal = null, decimal? PorcentajeIva = null, decimal? MontoIva = null);

/// <summary>Resumen de una reserva propia, sin datos de pasajeros.</summary>
public sealed record ReservaResumenResponse(
    int ReservaId, string CodigoReserva, int VueloId, string NumeroVuelo,
    DateTimeOffset FechaReserva, decimal MontoTotal, string Moneda, string Estado, string RowVersion,
    DateTimeOffset? FechaExpiracion = null, bool AnteriorAlFlujoTemporal = false,
    decimal? Subtotal = null, decimal? PorcentajeIva = null, decimal? MontoIva = null);

/// <summary>Página de reservas del usuario autenticado.</summary>
public sealed record PaginaReservasResponse(
    IReadOnlyList<ReservaResumenResponse> Reservas, int Pagina, int TamanoPagina, int Total, int TotalPaginas);
