using Microsoft.AspNetCore.Identity;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Models.Common;
using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Models.Usuarios.DTOs;
namespace Vuelos_Backend.Services.Usuarios;

public sealed class UsuarioService(
    IUsuarioRepository usuarios, IPasswordHasher<Usuario> passwordHasher, TimeProvider clock) : IUsuarioService
{
    public async Task<UsuarioResponse> RegistrarAsync(
        RegistroUsuarioRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await usuarios.ExisteCorreoAsync(email, cancellationToken))
            throw new CorreoDuplicadoException();

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = email,
            Rol = "cliente",
            FechaCreacion = clock.GetUtcNow().UtcDateTime
        };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);
        await usuarios.CrearAsync(usuario, cancellationToken);
        return new UsuarioResponse(usuario.UsuarioId, usuario.Nombre, usuario.Apellido, usuario.Email);
    }
}
