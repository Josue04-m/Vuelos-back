using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Controllers.Catalogos;

[ApiController]
[Authorize(Policy = PoliticasAcceso.ClienteOAdministrador)]
[Route("api/catalogos")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class CatalogosBusquedaController(IBusquedaCatalogosRepository catalogos) : ControllerBase
{
    /// <summary>Busca aeropuertos activos para un selector. Q busca por IATA, ICAO, nombre, ciudad o país; varias palabras deben coincidir todas.</summary>
    /// <remarks>Devuelve como máximo 50 resultados con datos mínimos. No descargue el catálogo completo: filtre en el servidor.</remarks>
    [HttpGet("aeropuertos")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    [ProducesResponseType<IReadOnlyList<AeropuertoResumenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AeropuertoResumenResponse>>> BuscarAeropuertos(
        [FromQuery] BusquedaCatalogoRequest request, CancellationToken ct) =>
        Ok(await catalogos.BuscarAeropuertosAsync(request.Q, Math.Min(request.Limite, 50), ct));

    /// <summary>Consulta un aeropuerto por ID, también inactivo, para rotular un vuelo o una selección guardada.</summary>
    [HttpGet("aeropuertos/{id:int}")]
    [ProducesResponseType<AeropuertoResumenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AeropuertoResumenResponse>> ObtenerAeropuerto([Range(1, int.MaxValue)] int id, CancellationToken ct) =>
        await catalogos.ObtenerAeropuertoAsync(id, ct) is { } aeropuerto ? Ok(aeropuerto) : NotFound(new ProblemDetails
        { Status = 404, Title = "No encontrado", Detail = "Aeropuerto no encontrado." });

    /// <summary>Busca aviones activos para un selector. Q busca por modelo, matrícula, IATA o ICAO.</summary>
    [HttpGet("aviones")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    [ProducesResponseType<IReadOnlyList<AvionResumenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvionResumenResponse>>> BuscarAviones(
        [FromQuery] BusquedaCatalogoRequest request, CancellationToken ct) =>
        Ok(await catalogos.BuscarAvionesAsync(request.Q, request.Limite, ct));
}
