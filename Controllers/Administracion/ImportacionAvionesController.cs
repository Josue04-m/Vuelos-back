using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Services.Catalogos;

namespace Vuelos_Backend.Controllers.Administracion;

[ApiController, Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
[Route("api/admin/aviones/importacion")]
[Consumes("multipart/form-data"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(2 * ImportacionAvionesService.MaxBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = 2 * ImportacionAvionesService.MaxBytes)]
[ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(413)]
public sealed class ImportacionAvionesController(IImportacionAvionesService service) : ControllerBase
{
    /// <summary>Valida CSV y categorías, sin guardar datos. Devuelve filas, errores y matrículas registradas.</summary>
    /// <remarks>CSV UTF-8 con ; y encabezados. Máximo 1 MiB, 500 aviones y 100000 asientos. No certifica capacidades reales. Puede devolver ya_importados sin crear registros.</remarks>
    [HttpPost("validar")]
    [ProducesResponseType<ImportacionAvionesResponse>(200)]
    public async Task<ActionResult<ImportacionAvionesResponse>> Validar([FromForm] ImportarAvionesRequest request, CancellationToken ct) =>
        Ok(await service.ValidarAsync(request, ct));

    /// <summary>Importa todo el CSV y genera asientos en una transacción. Repetir matrículas no crea duplicados.</summary>
    /// <remarks>Revalida bajo bloqueo administrativo. 200: importado o ya_importados; 400: errores de datos; 409: algunas matrículas ya existen. Reenviar el mismo archivo y las clases de la vista previa.</remarks>
    [HttpPost]
    [ProducesResponseType<ImportacionAvionesResponse>(200)]
    [ProducesResponseType<ImportacionAvionesResponse>(400)]
    [ProducesResponseType<ImportacionAvionesResponse>(409)]
    public async Task<ActionResult<ImportacionAvionesResponse>> Importar([FromForm] ImportarAvionesRequest request, CancellationToken ct)
    {
        var response = await service.ImportarAsync(request, ct);
        return response.Estado switch
        {
            "errores_validacion" => BadRequest(response),
            "conflicto_matriculas" => Conflict(response),
            _ => Ok(response)
        };
    }
}
