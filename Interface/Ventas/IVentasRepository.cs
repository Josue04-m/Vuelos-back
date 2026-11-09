using Vuelos_Backend.Models.Ventas.DTOs;

namespace Vuelos_Backend.Interface.Ventas;

public interface IVentasRepository
{
    Task<AcumuladoVentas> AcumularAsync(DateTime? desdeUtc, DateTime? hastaUtcExclusiva, CancellationToken ct, string entorno = "Sandbox");
}
