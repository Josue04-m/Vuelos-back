using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Interface.Catalogos;

public interface IImportacionAvionesService
{
    Task<ImportacionAvionesResponse> ValidarAsync(ImportarAvionesRequest request, CancellationToken ct);
    Task<ImportacionAvionesResponse> ImportarAsync(ImportarAvionesRequest request, CancellationToken ct);
}
