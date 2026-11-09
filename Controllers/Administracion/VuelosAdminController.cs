using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Controllers.Administracion;

[ApiController]
[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
[Route("api/admin/vuelos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class VuelosAdminController(IVueloAdminService administracion, IVueloService consultas) : ControllerBase
{
    /// <summary>Lista vuelos de todos los estados, con filtros y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaVuelosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaVuelosResponse>> Buscar(
        [FromQuery] BusquedaVuelosRequest request, CancellationToken cancellationToken) =>
        Ok(await consultas.BuscarAsync(request, cancellationToken));

    /// <summary>Consulta un vuelo y la versión necesaria para editar su programación.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<VueloAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VueloAdminResponse>> Obtener(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok(await administracion.ObtenerAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<VueloAdminResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VueloAdminResponse>> Crear(CrearVueloAdminRequest request, CancellationToken cancellationToken)
    {
        var administradorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
        var vuelo = await administracion.CrearAsync(request, administradorId, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = vuelo.Vuelo.VueloId }, vuelo);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<VueloAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VueloAdminResponse>> Editar(
        [Range(1, int.MaxValue)] int id, EditarVueloAdminRequest request, CancellationToken cancellationToken) =>
        Ok(await administracion.EditarAsync(id, request, cancellationToken));

    /// <summary>Cambia el avión de un vuelo sin retenciones vigentes, compras ni reservas antiguas; aplica tarifas por clase.</summary>
    [HttpPatch("{id:int}/avion")]
    [ProducesResponseType<VueloAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VueloAdminResponse>> CambiarAvion([Range(1, int.MaxValue)] int id,
        CambiarAvionVueloRequest request, CancellationToken ct)
    {
        var actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
        return Ok(await administracion.CambiarAvionAsync(id, request, actor, ct));
    }

    [HttpPost("{id:int}/estado")]
    [ProducesResponseType<VueloAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VueloAdminResponse>> CambiarEstado(
        [Range(1, int.MaxValue)] int id, CambiarEstadoVueloAdminRequest request, CancellationToken cancellationToken)
    {
        var administradorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
        return Ok(await administracion.CambiarEstadoAsync(id, request, administradorId, cancellationToken));
    }

    [HttpGet("{id:int}/historial")]
    [ProducesResponseType<IReadOnlyList<HistorialEstadoVueloAdminResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistorialEstadoVueloAdminResponse>>> ObtenerHistorial(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok(await administracion.ObtenerHistorialAsync(id, cancellationToken));

    /// <summary>Lista todos los asientos del vuelo con su disponibilidad, precio base y estado de reserva.</summary>
    [HttpGet("{id:int}/asientos")]
    [ProducesResponseType<IReadOnlyList<AsientoVueloAdminResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AsientoVueloAdminResponse>>> ListarAsientos(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok(await administracion.ListarAsientosAsync(id, cancellationToken));

    /// <summary>Actualiza el precio base o la disponibilidad (bloqueo/desbloqueo) de un asiento del vuelo.</summary>
    /// <remarks>No permite cambiar la disponibilidad de asientos con reservas activas. Exige RowVersion en Base64.</remarks>
    [HttpPatch("{id:int}/asientos/{asientoVueloId:int}")]
    [ProducesResponseType<AsientoVueloAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AsientoVueloAdminResponse>> ActualizarAsiento(
        [Range(1, int.MaxValue)] int id,
        [Range(1, int.MaxValue)] int asientoVueloId,
        ActualizarAsientoVueloAdminRequest request,
        CancellationToken cancellationToken) =>
        Ok(await administracion.ActualizarAsientoAsync(id, asientoVueloId, request, cancellationToken));

    [HttpGet("{id:int}/reservas")]
    [ProducesResponseType<IReadOnlyList<ReservaVueloAdminResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservaVueloAdminResponse>>> ListarReservas(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok(await administracion.ListarReservasAsync(id, cancellationToken));
}
