using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Autenticacion;
using Vuelos_Backend.Models.Autenticacion;
namespace Vuelos_Backend.Repositories.Autenticacion;

public sealed class TokenRepository(AppDbContext context) : ITokenRepository
{
    public async Task CrearAsync(TokenSesion sesion, CancellationToken cancellationToken)
    {
        context.Tokens.Add(sesion);
        // Contexto scoped compartido: guarda también un posible rehash del usuario.
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<TokenSesion?> BuscarVigenteAsync(
        string token, DateTime ahoraUtc, CancellationToken cancellationToken) =>
        context.Tokens.AsNoTracking().Include(x => x.Usuario)
            .SingleOrDefaultAsync(x => x.Token == token && x.Activo &&
                x.FechaExpiracion > ahoraUtc && !x.Usuario.Eliminado, cancellationToken);

    public async Task RevocarAsync(int sesionId, int usuarioId, CancellationToken cancellationToken)
    {
        // UPDATE idempotente: nunca elimina filas ni revoca otras sesiones.
        await context.Tokens.Where(x => x.Id == sesionId && x.UsuarioId == usuarioId && x.Activo)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Activo, false), cancellationToken);
    }
}
