using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Models.Usuarios.DTOs;
namespace Vuelos_Backend.Controllers.Usuarios;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("registro")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Registrar(
        RegistroUsuarioRequest request, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }
}
