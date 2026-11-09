using Vuelos_Backend.Interface.Reservas;

namespace Vuelos_Backend.Services.Reservas;

public sealed class ExpiracionReservasService(IReservaRepository reservas, ICicloReservaRepository ciclo, TimeProvider clock) : IExpiracionReservasService
{
    public async Task ExpirarVueloAsync(int vueloId, CancellationToken ct)
    {
        try
        {
            await using var transaction = await reservas.IniciarTransaccionAsync(ct);
            var vuelo = await reservas.BloquearVueloAsync(vueloId, ct);
            if (vuelo is not null)
            {
                var ahora = clock.GetUtcNow().UtcDateTime;
                var cerrado = vuelo.Estado is not ("programado" or "retrasado") || vuelo.SalidaReal.HasValue ||
                    vuelo.LlegadaReal.HasValue || vuelo.SalidaProgramada <= ahora;
                await ciclo.ExpirarPendientesAsync(vueloId, ahora, cerrado, ct);
            }
            await transaction.ConfirmarAsync(ct);
        }
        finally { ciclo.LimpiarSeguimiento(); } // No reutilizar entidades de una transacción anterior al adquirir un nuevo bloqueo.
    }

    public async Task ExpirarLoteAsync(int? usuarioId, CancellationToken ct)
    {
        IReadOnlyList<int> vuelos;
        do
        {
            vuelos = await ciclo.ObtenerVuelosVencidosAsync(clock.GetUtcNow().UtcDateTime, usuarioId, ct);
            foreach (var vueloId in vuelos) await ExpirarVueloAsync(vueloId, ct);
            // El worker mantiene lotes acotados. Las consultas propias completan todos sus lotes vencidos.
        } while (usuarioId.HasValue && vuelos.Count == 100);
    }
}
