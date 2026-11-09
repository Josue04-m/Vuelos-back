using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Repositories.Reservas;

public sealed class CicloReservaRepository(AppDbContext context, TimeProvider clock) : ICicloReservaRepository
{
    public bool EnTransaccion => context.Database.CurrentTransaction is not null;
    public void LimpiarSeguimiento() => context.ChangeTracker.Clear();
    public Task ExpirarPendientesAsync(int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct) =>
        ExpirarEnContextoAsync(context, vueloId, ahoraUtc, forzar, ct);

    // El llamador debe mantener el bloqueo UPDLOCK/HOLDLOCK del vuelo hasta confirmar la transacción.
    internal static async Task ExpirarEnContextoAsync(AppDbContext context, int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct)
    {
        try { await ExpirarCoreAsync(context, vueloId, ahoraUtc, forzar, ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "version_desactualizada", "La reserva cambió durante su liberación. Consulte el estado actual.");
        }
        catch (Exception ex) when ((ex as SqlException ?? ex.InnerException as SqlException)?.Number == 1205)
        {
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "operacion_concurrente", "La liberación compitió con otra operación. Consulte el estado actual.");
        }
    }

    private static async Task ExpirarCoreAsync(AppDbContext context, int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct)
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("La expiración requiere transacción y bloqueo del vuelo.");
        var reservas = await context.Reservas.Where(r => r.VueloId == vueloId && r.Estado == "pendiente_pago" && !context.PagosPayPal.Any(p => p.ReservaId == r.ReservaId && p.BloqueaReserva) &&
            (forzar || r.FechaExpiracion <= ahoraUtc)).OrderBy(r => r.ReservaId).ToListAsync(ct);
        if (reservas.Count == 0) return;
        var ids = reservas.Select(r => r.ReservaId).ToArray();
        var detalles = await context.DetallesReserva.Where(d => ids.Contains(d.ReservaId) && d.Estado == "activo").ToListAsync(ct);
        var asientoIds = detalles.Select(d => d.AsientoVueloId).Distinct().ToArray();
        var asientos = await context.AsientosVuelo.Where(a => asientoIds.Contains(a.AsientoVueloId)).OrderBy(a => a.AsientoVueloId).ToListAsync(ct);
        foreach (var r in reservas)
        {
            r.Estado = "expirada";
            context.HistorialCambiosReserva.Add(new HistorialCambioReserva { ReservaId = r.ReservaId, TipoAccion = "EXPIRACION",
                FechaAccion = ahoraUtc, Observacion = forzar ? "Fin del plazo de compra por cierre operativo del vuelo." : "Venció el plazo de compra de la reserva." });
        }
        foreach (var d in detalles) d.Estado = "liberado";
        foreach (var a in asientos) if (a.Estado == "reservado") a.Estado = "disponible";
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<int>> ObtenerVuelosVencidosAsync(DateTime ahoraUtc, int? usuarioId, CancellationToken ct) =>
        await context.Reservas.AsNoTracking().Where(r => r.Estado == "pendiente_pago" && !context.PagosPayPal.Any(p => p.ReservaId == r.ReservaId && p.BloqueaReserva) &&
            (!usuarioId.HasValue || r.UsuarioId == usuarioId.Value) &&
            (r.FechaExpiracion <= ahoraUtc || r.Vuelo.SalidaProgramada <= ahoraUtc || r.Vuelo.SalidaReal != null ||
             r.Vuelo.LlegadaReal != null || r.Vuelo.Estado != "programado" && r.Vuelo.Estado != "retrasado"))
            .Select(r => r.VueloId).Distinct().OrderBy(id => id).Take(100).ToListAsync(ct);

    public Task<CompraSimulada?> ObtenerCompraAsync(int reservaId, CancellationToken ct) =>
        context.ComprasSimuladas.AsNoTracking().SingleOrDefaultAsync(c => c.ReservaId == reservaId, ct);
    public Task<bool> PagoImpideCambiosAsync(int reservaId, CancellationToken ct, bool soloCaptura = false) =>
        context.PagosPayPal.AnyAsync(p => p.ReservaId == reservaId &&
            (p.BloqueaReserva || p.FechaCaptura != null || !soloCaptura && p.Estado != "fallido" && p.Reserva.Estado == "pendiente_pago" && p.Reserva.FechaExpiracion > clock.GetUtcNow().UtcDateTime), ct);
}
