using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Controllers.Catalogos;

[ApiController, Route("api/aeropuertos")]
public sealed class AeropuertosController(IAeropuertosPublicosRepository aeropuertos) : ControllerBase
{
    /// <summary>Busca aeropuertos activos por país en español o código ISO, sin iniciar sesión.</summary>
    /// <remarks>Ejemplos: Ecuador, EC, Perú, Peru, Ecua. Solo devuelve aeropuertos con IATA válido.</remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PaginaAeropuertosPublicosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaAeropuertosPublicosResponse>> Buscar(
        [FromQuery] BuscarAeropuertosPublicosRequest request, CancellationToken ct) =>
        Ok(await aeropuertos.BuscarAsync(request, ct));
}
