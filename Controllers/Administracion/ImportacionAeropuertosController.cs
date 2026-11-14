using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Controllers.Administracion;

[ApiController, Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
[Route("api/admin/aeropuertos/importacion")]
[Consumes("multipart/form-data"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(24 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 24 * 1024 * 1024)]
[ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(403), ProducesResponseType<ProblemDetails>(413)]
public sealed class ImportacionAeropuertosController(IImportacionAeropuertosService service) : ControllerBase
{
    /// <summary>Valida airports.csv sin guardar. Vista previa paginada con filtros, omisiones y errores.</summary>
    /// <remarks>CSV UTF-8 con comas y encabezados originales. Máximo 20 MiB, 150000 registros y 20000 importables. Por defecto solo servicio regular; sin IATA se omite. Incompletos bloquean salvo autorización explícita. La vista previa devuelve hasta 200 filas por página y hasta 200 errores.</remarks>
    [HttpPost("validar"), ProducesResponseType<ImportacionAeropuertosResponse>(200)]
    public async Task<ActionResult<ImportacionAeropuertosResponse>> Validar([FromForm] ImportarAeropuertosRequest request, CancellationToken ct) =>
        Ok(await service.ValidarAsync(request, ct));

    /// <summary>Importa toda la selección en una transacción. No sobrescribe ni duplica códigos IATA existentes.</summary>
    /// <remarks>Reenviar archivo y filtros revisados. Pagina/TamanoPagina afectan solo la respuesta. Los IATA existentes se omiten automáticamente, incluso si están inactivos. 200 importado/ya_importados; 400 errores reales. No importa únicamente la página visible.</remarks>
    [HttpPost, ProducesResponseType<ImportacionAeropuertosResponse>(200)]
    [ProducesResponseType<ImportacionAeropuertosResponse>(400), ProducesResponseType<ImportacionAeropuertosResponse>(409)]
    public async Task<ActionResult<ImportacionAeropuertosResponse>> Importar([FromForm] ImportarAeropuertosRequest request, CancellationToken ct)
    {
        var response = await service.ImportarAsync(request, ct);
        return response.Estado switch
        {
            "errores_validacion" => BadRequest(response),
            _ => Ok(response)
        };
    }
}
