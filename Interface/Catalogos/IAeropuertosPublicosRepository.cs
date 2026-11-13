using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Interface.Catalogos;

public interface IAeropuertosPublicosRepository
{
    Task<PaginaAeropuertosPublicosResponse> BuscarAsync(BuscarAeropuertosPublicosRequest request, CancellationToken ct);
}
