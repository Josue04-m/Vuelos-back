using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Autenticacion.DTOs;
namespace Vuelos_Backend.Interface.Autenticacion;

public interface IAutenticacionService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<SesionAutenticada?> ValidarTokenAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(int sesionId, int usuarioId, CancellationToken cancellationToken);
}
