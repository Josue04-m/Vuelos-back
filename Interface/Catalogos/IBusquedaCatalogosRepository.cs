using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Interface.Catalogos;

public interface IBusquedaCatalogosRepository
{
    Task<IReadOnlyList<AeropuertoResumenResponse>> BuscarAeropuertosAsync(string? q, int limite, CancellationToken ct);
    /// <summary>Incluye aeropuertos inactivos para poder rotular vuelos históricos.</summary>
    Task<AeropuertoResumenResponse?> ObtenerAeropuertoAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<AvionResumenResponse>> BuscarAvionesAsync(string? q, int limite, CancellationToken ct);
}
