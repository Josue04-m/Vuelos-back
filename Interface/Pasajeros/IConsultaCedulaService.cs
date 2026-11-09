using Vuelos_Backend.Models.Pasajeros.DTOs;
namespace Vuelos_Backend.Interface.Pasajeros;

public interface IConsultaCedulaService
{
    Task<ConsultaCedulaResponse> ConsultarAsync(string cedula, CancellationToken cancellationToken);
}
