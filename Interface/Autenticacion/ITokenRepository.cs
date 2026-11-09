using Vuelos_Backend.Models.Autenticacion;
namespace Vuelos_Backend.Interface.Autenticacion;

public interface ITokenRepository
{
    Task CrearAsync(TokenSesion sesion, CancellationToken cancellationToken);
    Task<TokenSesion?> BuscarVigenteAsync(string token, DateTime ahoraUtc, CancellationToken cancellationToken);
    Task RevocarAsync(int sesionId, int usuarioId, CancellationToken cancellationToken);
}
