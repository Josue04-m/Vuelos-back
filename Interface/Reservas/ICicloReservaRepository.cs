using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Interface.Reservas;

public interface ICicloReservaRepository
{
    bool EnTransaccion { get; }
    void LimpiarSeguimiento();
    Task ExpirarPendientesAsync(int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct);
    Task<IReadOnlyList<int>> ObtenerVuelosVencidosAsync(DateTime ahoraUtc, int? usuarioId, CancellationToken ct);
    Task<CompraSimulada?> ObtenerCompraAsync(int reservaId, CancellationToken ct);
    Task<bool> PagoImpideCambiosAsync(int reservaId, CancellationToken ct, bool soloCaptura = false);
}
