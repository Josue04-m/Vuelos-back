using System.Data.SqlTypes;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Autenticacion;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Autenticacion.DTOs;
using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Models.Usuarios.DTOs;
namespace Vuelos_Backend.Services.Autenticacion;

public sealed class AutenticacionService(
    IUsuarioRepository usuarios,
    ITokenRepository tokens,
    IPasswordHasher<Usuario> passwordHasher,
    IOptions<OpcionesSesion> opciones,
    TimeProvider clock) : IAutenticacionService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.BuscarPorCorreoAsync(
            request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (usuario is null || usuario.Eliminado)
            return null;

        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (resultado == PasswordVerificationResult.Failed)
            return null;
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);

        // DATETIME redondea a 1/300 s: devolver exactamente la expiración persistida.
        var ahoraUtc = new SqlDateTime(clock.GetUtcNow().UtcDateTime).Value;
        var expiracionUtc = new SqlDateTime(
            ahoraUtc.AddMinutes(opciones.Value.DuracionTokenMinutos)).Value;
        var sesion = new TokenSesion
        {
            UsuarioId = usuario.UsuarioId,
            Usuario = usuario,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            FechaCreacion = ahoraUtc,
            FechaExpiracion = expiracionUtc,
            Activo = true
        };
        await tokens.CrearAsync(sesion, cancellationToken);
        return new LoginResponse(sesion.Token, "Bearer",
            new DateTimeOffset(DateTime.SpecifyKind(expiracionUtc, DateTimeKind.Utc)),
            new UsuarioResponse(usuario.UsuarioId, usuario.Nombre, usuario.Apellido, usuario.Email));
    }

    public async Task<SesionAutenticada?> ValidarTokenAsync(string token, CancellationToken cancellationToken)
    {
        // Los tokens emitidos usan hexadecimal en mayúsculas. Evita que una
        // collation SQL no sensible a mayúsculas acepte representaciones diferentes.
        if (token.Length != 64 || token.Any(c => !(c is >= '0' and <= '9' or >= 'A' and <= 'F')))
            return null;
        var sesion = await tokens.BuscarVigenteAsync(token, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (sesion is null || !sesion.Activo || sesion.Usuario.Eliminado ||
            sesion.FechaExpiracion <= clock.GetUtcNow().UtcDateTime ||
            !string.Equals(sesion.Token, token, StringComparison.Ordinal))
            return null;

        return new SesionAutenticada(sesion.Id, sesion.UsuarioId, sesion.Usuario.Rol);
    }

    public Task LogoutAsync(int sesionId, int usuarioId, CancellationToken cancellationToken) =>
        tokens.RevocarAsync(sesionId, usuarioId, cancellationToken);
}
