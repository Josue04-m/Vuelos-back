using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Vuelos;
namespace Vuelos_Backend.Interface.Vuelos;

public interface IVueloRepository
{
    Task<ResultadoBusquedaVuelos> BuscarAsync(FiltroVuelos filtro, CancellationToken cancellationToken);
    Task<Vuelo?> ObtenerAsync(int vueloId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsientoVuelo>> ListarAsientosAsync(int vueloId, CancellationToken cancellationToken);
}
