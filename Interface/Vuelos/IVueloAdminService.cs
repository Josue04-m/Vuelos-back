using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Interface.Vuelos;

public interface IVueloAdminService
{
    Task<VueloAdminResponse> ObtenerAsync(int vueloId, CancellationToken cancellationToken);
    Task<VueloAdminResponse> CrearAsync(CrearVueloAdminRequest request, int administradorId, CancellationToken cancellationToken);
    Task<VueloAdminResponse> EditarAsync(int vueloId, EditarVueloAdminRequest request, CancellationToken cancellationToken);
    Task<VueloAdminResponse> CambiarAvionAsync(int vueloId, CambiarAvionVueloRequest request, int administradorId, CancellationToken ct);
    Task<VueloAdminResponse> CambiarEstadoAsync(int vueloId, CambiarEstadoVueloAdminRequest request, int administradorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HistorialEstadoVueloAdminResponse>> ObtenerHistorialAsync(int vueloId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsientoVueloAdminResponse>> ListarAsientosAsync(int vueloId, CancellationToken cancellationToken);
    Task<AsientoVueloAdminResponse> ActualizarAsientoAsync(int vueloId, int asientoVueloId, ActualizarAsientoVueloAdminRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReservaVueloAdminResponse>> ListarReservasAsync(int vueloId, CancellationToken cancellationToken);
}
