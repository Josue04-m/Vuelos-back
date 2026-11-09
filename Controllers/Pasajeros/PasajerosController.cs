using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Interface.Pasajeros;
using Vuelos_Backend.Models.Pasajeros.DTOs;
namespace Vuelos_Backend.Controllers.Pasajeros;

[ApiController]
[Authorize]
[Route("api/pasajeros")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class PasajerosController(IConsultaCedulaService consulta) : ControllerBase
{
    [HttpGet("consulta-cedula/{cedula}")]
    [ProducesResponseType<ConsultaCedulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ConsultaCedulaResponse>> ConsultarCedula(
        [StringLength(10, MinimumLength = 10), RegularExpression("^[0-9]{10}$")] string cedula,
        CancellationToken cancellationToken) =>
        Ok(await consulta.ConsultarAsync(cedula, cancellationToken));
}
