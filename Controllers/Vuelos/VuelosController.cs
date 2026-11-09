using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Asientos.DTOs;
using Vuelos_Backend.Models.Vuelos.DTOs;
using Vuelos_Backend.Authentication;
namespace Vuelos_Backend.Controllers.Vuelos;

[ApiController]
[Authorize(Policy = PoliticasAcceso.ClienteOAdministrador)]
[Route("api/vuelos")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class VuelosController(IVueloService vuelos) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaVuelosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaVuelosResponse>> Buscar(
        [FromQuery] BusquedaVuelosRequest request, CancellationToken cancellationToken) =>
        Ok(await vuelos.BuscarAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<VueloResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VueloResponse>> Obtener(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
    {
        var vuelo = await vuelos.ObtenerAsync(id, cancellationToken);
        if (vuelo is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Vuelo no encontrado");
        return Ok(vuelo);
    }

    [HttpGet("{id:int}/asientos")]
    [ProducesResponseType<AsientosVueloResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsientosVueloResponse>> Asientos(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken)
    {
        var asientos = await vuelos.ListarAsientosAsync(id, cancellationToken);
        if (asientos is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Vuelo no encontrado");
        return Ok(asientos);
    }
}
