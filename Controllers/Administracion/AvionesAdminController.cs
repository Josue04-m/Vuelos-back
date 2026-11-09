using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Controllers.Administracion;

[ApiController]
[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
[Route("api/admin/aviones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class AvionesAdminController(ICatalogosAdminService catalogos) : ControllerBase
{
    /// <summary>Lista aviones con búsqueda, estado y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaCatalogoResponse<AvionAdminResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaCatalogoResponse<AvionAdminResponse>>> Listar(
        [FromQuery] FiltroCatalogoRequest filtro, CancellationToken ct) =>
        Ok(await catalogos.ListarAvionesAsync(filtro, ct));

    /// <summary>Consulta un elemento, incluidos los inactivos, y su versión vigente.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AvionAdminResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AvionAdminResponse>> Obtener([Range(1, int.MaxValue)] int id, CancellationToken ct) =>
        Ok(await catalogos.ObtenerAvionAsync(id, ct));

}
