using Vuelos_Backend.Models.Ventas.DTOs;

namespace Vuelos_Backend.Interface.Ventas;

public interface IVentasService
{
    Task<ResumenVentasResponse> ObtenerResumenAsync(FiltroVentasRequest filtro, CancellationToken ct);
}
