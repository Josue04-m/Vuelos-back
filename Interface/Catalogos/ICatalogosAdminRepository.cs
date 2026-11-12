using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Models.Common;

namespace Vuelos_Backend.Interface.Catalogos;

public interface ITransaccionCatalogo : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct);
}

public interface ICatalogosAdminRepository
{
    Task<ITransaccionCatalogo> IniciarTransaccionAsync(CancellationToken ct);
    Task<(IReadOnlyList<T> Elementos, int Total)> BuscarAsync<T>(FiltroCatalogoRequest filtro, CancellationToken ct) where T : class, ICatalogoAdministrable;
    Task<T?> ObtenerAsync<T>(int id, CancellationToken ct) where T : class, ICatalogoAdministrable;
    Task CrearAsync<T>(T entity, CancellationToken ct) where T : class, ICatalogoAdministrable;
    Task CrearAsientosAsync(IReadOnlyList<Vuelos_Backend.Models.Asientos.AsientoFisico> asientos, CancellationToken ct);
    Task<IReadOnlyList<Vuelos_Backend.Models.Aviones.Avion>> ObtenerPorMatriculasAsync(string[] matriculas, CancellationToken ct);
    void LimpiarSeguimientoImportacion();
    Task<IReadOnlyList<Vuelos_Backend.Models.Aeropuertos.Aeropuerto>> ObtenerPorCodigosIataAsync(string[] codigos, CancellationToken ct);
    Task CrearAeropuertosAsync(IReadOnlyList<Vuelos_Backend.Models.Aeropuertos.Aeropuerto> aeropuertos, CancellationToken ct);
}
