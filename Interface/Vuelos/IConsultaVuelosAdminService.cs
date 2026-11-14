using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Interface.Vuelos;

public interface IConsultaVuelosAdminService
{
    Task<PaginaVuelosResponse> BuscarAsync(BusquedaVuelosAdminRequest request, CancellationToken ct);
    Task<RecorridoVueloAdminResponse?> ObtenerRecorridoAsync(int vueloId, CancellationToken ct);
}
