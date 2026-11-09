namespace Vuelos_Backend.Models.Vuelos;

// Criterios normalizados para persistencia; el límite superior de fechas es exclusivo.
public sealed record FiltroVuelos(
    string? Origen, string? Destino, DateTime? SalidaDesdeUtc, DateTime? SalidaHastaUtcExclusiva,
    int Pagina, int TamanoPagina);

public sealed record ResultadoBusquedaVuelos(IReadOnlyList<Vuelo> Vuelos, int Total);
