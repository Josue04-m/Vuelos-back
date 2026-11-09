using Vuelos_Backend.Models.Usuarios.DTOs;
namespace Vuelos_Backend.Interface.Usuarios;

public interface IUsuarioService
{
    Task<UsuarioResponse> RegistrarAsync(RegistroUsuarioRequest request, CancellationToken cancellationToken);
}
