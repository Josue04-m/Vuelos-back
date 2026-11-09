using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Autenticacion;
using Vuelos_Backend.Models.Autenticacion.DTOs;
namespace Vuelos_Backend.Controllers.Autenticacion;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(IAutenticacionService autenticacion) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        var sesion = await autenticacion.LoginAsync(request, cancellationToken);
        if (sesion is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales no válidas", detail: "El correo o la contraseña no son válidos.");
        return Ok(sesion);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
        var sesionId = int.Parse(User.FindFirstValue(TokenAuthenticationHandler.SesionIdClaim)!, CultureInfo.InvariantCulture);
        await autenticacion.LogoutAsync(sesionId, usuarioId, cancellationToken);
        return NoContent();
    }
}
