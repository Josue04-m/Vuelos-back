using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;
namespace Vuelos_Backend.Interface.Reservas;

public interface ITransaccionReserva : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken);
}

public interface IReservaRepository
{
    Task<ITransaccionReserva> IniciarTransaccionAsync(CancellationToken cancellationToken);
    Task<Vuelo?> BloquearVueloAsync(int vueloId, CancellationToken cancellationToken);
    Task<Reserva?> BloquearPropiaAsync(int reservaId, int usuarioId, CancellationToken cancellationToken);
    Task<int?> ObtenerVueloPropioAsync(int reservaId, int usuarioId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsientoVuelo>> ObtenerAsientosAsync(int vueloId, int[] asientos, CancellationToken cancellationToken);
    Task<IReadOnlyList<DetalleReserva>> ObtenerActivosAsync(int reservaId, CancellationToken cancellationToken);
    Task<int> CambiarEstadoAsientoAsync(int asientoId, int vueloId, string estadoAnterior, string estadoNuevo,
        byte[] rowVersion, CancellationToken cancellationToken);
    void AgregarReserva(Reserva reserva);
    void AgregarDetalle(DetalleReserva detalle);
    void AgregarHistorial(HistorialCambioReserva historial);
    void PrepararVersionReserva(Reserva reserva, byte[] version);
    Task GuardarAsync(CancellationToken cancellationToken);
    Task<ReservaLectura?> ObtenerPropiaAsync(int reservaId, int usuarioId, CancellationToken cancellationToken);
    Task<ResultadoBusquedaReservas> BuscarPropiasAsync(int usuarioId, FiltroReservas filtro, CancellationToken cancellationToken);
}
