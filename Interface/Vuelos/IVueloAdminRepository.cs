using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Interface.Vuelos;

public interface ITransaccionVueloAdmin : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken);
}

public interface IVueloAdminRepository
{
    Task<ITransaccionVueloAdmin> IniciarTransaccionAsync(CancellationToken cancellationToken);
    Task<Avion?> BloquearAvionAsync(int avionId, CancellationToken cancellationToken);
    Task<int?> ObtenerAvionIdAsync(int vueloId, CancellationToken cancellationToken);
    Task<Vuelo?> ObtenerAsync(int vueloId, bool bloquear, CancellationToken cancellationToken);
    Task<bool> ExisteAeropuertoAsync(int aeropuertoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsientoFisico>> ObtenerAsientosFisicosAsync(int avionId, CancellationToken cancellationToken);
    Task<bool> TieneReservasVigentesAsync(int vueloId, CancellationToken cancellationToken);
    Task<bool> TieneReservasRegistradasAsync(int vueloId, CancellationToken cancellationToken);
    Task ExpirarPendientesAsync(int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct);
    Task<IReadOnlyList<AsientoVuelo>> ObtenerTodosAsientosAsync(int vueloId, CancellationToken ct);
    void AgregarAsientos(IReadOnlyList<AsientoVuelo> asientos);
    Task<bool> TieneSolapamientoAsync(int avionId, int? excluirVueloId, DateTime salidaUtc,
        DateTime llegadaUtc, CancellationToken cancellationToken);
    Task<int> ContarAsientosAsync(int vueloId, CancellationToken cancellationToken);
    void Agregar(Vuelo vuelo, IReadOnlyList<AsientoVuelo> asientos, HistorialEstadoVuelo historial);
    void AgregarHistorial(HistorialEstadoVuelo historial);
    Task CancelarReservasYLiberarAsientosAsync(int vueloId, DateTime ahoraUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<HistorialEstadoVuelo>> ObtenerHistorialAsync(int vueloId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsientoVuelo>> ListarAsientosConDetallesAsync(int vueloId, CancellationToken cancellationToken);
    Task<HashSet<int>> ObtenerIdsAsientosConReservaActivaAsync(int vueloId, CancellationToken cancellationToken);
    Task<AsientoVuelo?> ObtenerAsientoVueloAsync(int vueloId, int asientoVueloId, bool bloquear, CancellationToken cancellationToken);
    Task<bool> AsientoTieneReservaActivaAsync(int asientoVueloId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reserva>> ObtenerReservasDeVueloAsync(int vueloId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DetalleReserva>> ObtenerDetallesDeReservasAsync(IEnumerable<int> reservaIds, CancellationToken cancellationToken);
    Task GuardarAsync(CancellationToken cancellationToken);
}
