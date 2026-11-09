using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Ventas;
using Vuelos_Backend.Models.Ventas.DTOs;

namespace Vuelos_Backend.Controllers.Administracion;

[ApiController]
[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
[Route("api/admin/ventas")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VentasAdminController(IVentasService ventas) : ControllerBase
{
    /// <summary>Consulta el total acumulado en USD y la cantidad de capturas PayPal confirmadas por entorno.</summary>
    /// <remarks>Por defecto Sandbox. Entorno=Live consulta cobros reales. Total bruto histórico; no descuenta reembolsos.</remarks>
    [HttpGet("resumen")]
    [ProducesResponseType<ResumenVentasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResumenVentasResponse>> Obtener([FromQuery] FiltroVentasRequest filtro, CancellationToken ct) =>
        Ok(await ventas.ObtenerResumenAsync(filtro, ct));
}
