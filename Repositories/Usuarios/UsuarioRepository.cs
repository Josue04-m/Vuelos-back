using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Models.Common;
using Vuelos_Backend.Models.Usuarios;
namespace Vuelos_Backend.Repositories.Usuarios;

public sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<bool> ExisteCorreoAsync(string email, CancellationToken cancellationToken) =>
        context.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Email.ToLower() == email, cancellationToken);

    // Se conserva tracking para actualizar hashes antiguos durante el login.
    public Task<Usuario?> BuscarPorCorreoAsync(string email, CancellationToken cancellationToken) =>
        context.Usuarios.SingleOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

    public async Task CrearAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        context.Usuarios.Add(usuario);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (ErroresPostgreSql.EsUnico(exception, "UQ_usuarios_email"))
        {
            // La UQ cubre también dos registros concurrentes con el mismo correo.
            context.Entry(usuario).State = EntityState.Detached;
            throw new CorreoDuplicadoException();
        }
    }
}
