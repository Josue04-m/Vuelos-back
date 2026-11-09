using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Reservas.DTOs;
namespace Vuelos_Backend.Controllers.Reservas;

[ApiController]
[Authorize]
[Route("api/reservas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ReservasController(IReservaService reservas) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    /// <summary>Crea una reserva temporal pendiente de pago. Retiene los asientos hasta FechaExpiracion.</summary>
    /// <remarks>Las nuevas reservas guardan subtotal después de descuentos, IVA configurable inicialmente 15% y montoTotal final. TotalEsperado debe coincidir con ese total; PayPal cobra montoTotal.</remarks>
    [HttpPost]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReservaResponse>> Crear(CrearReservaRequest request, CancellationToken cancellationToken)
    {
        var response = await reservas.CrearAsync(UsuarioId, request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = response.ReservaId }, response);
    }

    [HttpGet]
    [ProducesResponseType<PaginaReservasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaReservasResponse>> Buscar(
        [FromQuery] BusquedaReservasRequest request, CancellationToken cancellationToken) =>
        Ok(await reservas.BuscarPropiasAsync(UsuarioId, request, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservaResponse>> Obtener(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken) =>
        Ok(await reservas.ObtenerPropiaAsync(UsuarioId, id, cancellationToken));

    [HttpPatch("{id:int}/asientos")]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReservaResponse>> CambiarAsientos(
        [Range(1, int.MaxValue)] int id, CambiarAsientosRequest request, CancellationToken cancellationToken) =>
        Ok(await reservas.CambiarAsientosAsync(UsuarioId, id, request, cancellationToken));

    [HttpPost("{id:int}/cancelacion")]
    [ProducesResponseType<ReservaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaResponse>> Cancelar(
        [Range(1, int.MaxValue)] int id, CancelarReservaRequest request, CancellationToken cancellationToken) =>
        Ok(await reservas.CancelarAsync(UsuarioId, id, request, cancellationToken));
}
