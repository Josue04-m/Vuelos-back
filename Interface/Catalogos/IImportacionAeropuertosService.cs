using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Interface.Catalogos;

public interface IImportacionAeropuertosService
{
    Task<ImportacionAeropuertosResponse> ValidarAsync(ImportarAeropuertosRequest request, CancellationToken ct);
    Task<ImportacionAeropuertosResponse> ImportarAsync(ImportarAeropuertosRequest request, CancellationToken ct);
}
