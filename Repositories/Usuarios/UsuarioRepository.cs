using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Models.Common;
using Vuelos_Backend.Models.Usuarios;
namespace Vuelos_Backend.Repositories.Usuarios;

public sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<bool> ExisteCorreoAsync(string email, CancellationToken cancellationToken) =>
        context.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken);

    // Se conserva tracking para actualizar hashes antiguos durante el login.
    public Task<Usuario?> BuscarPorCorreoAsync(string email, CancellationToken cancellationToken) =>
        context.Usuarios.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public async Task CrearAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        context.Usuarios.Add(usuario);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            sql.Errors.Cast<SqlError>().Any(error =>
                (error.Number is 2601 or 2627) &&
                error.Message.Contains("UQ_usuarios_email", StringComparison.OrdinalIgnoreCase)))
        {
            // La UQ cubre también dos registros concurrentes con el mismo correo.
            context.Entry(usuario).State = EntityState.Detached;
            throw new CorreoDuplicadoException();
        }
    }
}
