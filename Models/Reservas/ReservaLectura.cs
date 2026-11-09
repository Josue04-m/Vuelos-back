namespace Vuelos_Backend.Models.Reservas;

public sealed record ReservaLectura(
    Reserva Reserva, IReadOnlyList<DetalleReserva> Detalles, IReadOnlyList<HistorialCambioReserva> Historial);

public sealed record FiltroReservas(
    string? Estado, DateTime? DesdeUtc, DateTime? HastaUtcExclusiva, int Pagina, int TamanoPagina);

public sealed record ResultadoBusquedaReservas(IReadOnlyList<Reserva> Reservas, int Total);
