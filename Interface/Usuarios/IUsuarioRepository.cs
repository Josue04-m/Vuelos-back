using Vuelos_Backend.Models.Usuarios;
namespace Vuelos_Backend.Interface.Usuarios;

public interface IUsuarioRepository
{
    Task<bool> ExisteCorreoAsync(string email, CancellationToken cancellationToken);
    Task<Usuario?> BuscarPorCorreoAsync(string email, CancellationToken cancellationToken);
    Task CrearAsync(Usuario usuario, CancellationToken cancellationToken);
}
