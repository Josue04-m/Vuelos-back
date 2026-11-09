using Vuelos_Backend.Models.Asientos.DTOs;
using Vuelos_Backend.Models.Vuelos.DTOs;
namespace Vuelos_Backend.Interface.Vuelos;

public interface IVueloService
{
    Task<PaginaVuelosResponse> BuscarAsync(BusquedaVuelosRequest request, CancellationToken cancellationToken);
    Task<VueloResponse?> ObtenerAsync(int vueloId, CancellationToken cancellationToken);
    Task<AsientosVueloResponse?> ListarAsientosAsync(int vueloId, CancellationToken cancellationToken);
}
