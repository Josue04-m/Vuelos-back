using Vuelos_Backend.Models.Reservas.DTOs;
namespace Vuelos_Backend.Interface.Reservas;

public interface IReservaService
{
    Task<ReservaResponse> CrearAsync(int usuarioId, CrearReservaRequest request, CancellationToken cancellationToken);
    Task<PaginaReservasResponse> BuscarPropiasAsync(int usuarioId, BusquedaReservasRequest request, CancellationToken cancellationToken);
    Task<ReservaResponse> ObtenerPropiaAsync(int usuarioId, int reservaId, CancellationToken cancellationToken);
    Task<ReservaResponse> CambiarAsientosAsync(int usuarioId, int reservaId, CambiarAsientosRequest request, CancellationToken cancellationToken);
    Task<ReservaResponse> CancelarAsync(int usuarioId, int reservaId, CancelarReservaRequest request, CancellationToken cancellationToken);
}
