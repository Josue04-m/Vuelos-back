using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Interface.Catalogos;

public interface ICatalogosAdminService
{
    Task<PaginaCatalogoResponse<AeropuertoAdminResponse>> ListarAeropuertosAsync(FiltroCatalogoRequest filtro, CancellationToken ct);
    Task<PaginaCatalogoResponse<AvionAdminResponse>> ListarAvionesAsync(FiltroCatalogoRequest filtro, CancellationToken ct);
    Task<PaginaCatalogoResponse<CategoriaAdminResponse>> ListarCategoriasAsync(FiltroCatalogoRequest filtro, CancellationToken ct);
    Task<PaginaCatalogoResponse<AsientoFisicoAdminResponse>> ListarAsientosAsync(FiltroCatalogoRequest filtro, CancellationToken ct);
    Task<AeropuertoAdminResponse> ObtenerAeropuertoAsync(int id, CancellationToken ct);
    Task<AvionAdminResponse> ObtenerAvionAsync(int id, CancellationToken ct);
    Task<CategoriaAdminResponse> ObtenerCategoriaAsync(int id, CancellationToken ct);
    Task<AsientoFisicoAdminResponse> ObtenerAsientoAsync(int id, CancellationToken ct);
}
