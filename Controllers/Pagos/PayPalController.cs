using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Models.Pagos;
using Vuelos_Backend.Services.Pagos;

namespace Vuelos_Backend.Controllers.Pagos;

[ApiController, Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(503)]
public sealed class PayPalController(PagoPayPalService pagos) : ControllerBase
{
    private int Usuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    [HttpPost("api/reservas/{id:int:min(1)}/paypal/orden")]
    [ProducesResponseType<PagoPayPalResponse>(200)]
    public async Task<ActionResult<PagoPayPalResponse>> Crear(int id, CrearOrdenPayPalRequest request, CancellationToken ct) =>
        Ok(await pagos.CrearAsync(Usuario, id, request, ct));

    [HttpPost("api/reservas/{id:int:min(1)}/paypal/captura")]
    [ProducesResponseType<PagoPayPalResponse>(200)]
    public async Task<ActionResult<PagoPayPalResponse>> Capturar(int id, CancellationToken ct) =>
        Ok(await pagos.CapturarAsync(Usuario, id, ct));

    /// <summary>Consulta el pago propio y concilia capturas inciertas con PayPal.</summary>
    [HttpGet("api/reservas/{id:int:min(1)}/paypal")]
    [ProducesResponseType<PagoPayPalResponse>(200)]
    public async Task<ActionResult<PagoPayPalResponse>> Obtener(int id, CancellationToken ct) =>
        Ok(await pagos.ConsultarAsync(Usuario, id, ct));

    [Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
    [HttpPost("api/admin/pagos/paypal/{pagoId:guid}/conciliacion")]
    [ProducesResponseType(204), ProducesResponseType<ProblemDetails>(403)]
    public async Task<IActionResult> Conciliar(Guid pagoId, CancellationToken ct)
    { await pagos.ConciliarAsync(pagoId, ct); return NoContent(); }

    [AllowAnonymous, HttpPost("api/pagos/paypal/webhook"), RequestSizeLimit(262144)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Webhook([FromBody] JsonElement evento, CancellationToken ct)
    { await pagos.ProcesarEventoAsync(evento, Request.Headers, ct); return NoContent(); }
}
