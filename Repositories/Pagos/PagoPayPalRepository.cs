using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Pagos;
using Vuelos_Backend.Models.Pagos;

namespace Vuelos_Backend.Repositories.Pagos;

public sealed class PagoPayPalRepository(AppDbContext db) : IPagoPayPalRepository
{
    // Las escrituras son confirmadas por IReservaRepository dentro de su bloqueo compartido del vuelo.
    public Task<PagoPayPal?> PorReservaAsync(int id, CancellationToken ct) =>
        db.PagosPayPal.Include(p => p.Reserva).SingleOrDefaultAsync(p => p.ReservaId == id, ct);
    public Task<PagoPayPal?> PorIdAsync(Guid id, CancellationToken ct) =>
        db.PagosPayPal.Include(p => p.Reserva).SingleOrDefaultAsync(p => p.PagoId == id, ct);
    public Task<PagoPayPal?> PorIdentificadorExternoAsync(string orden, string captura, string entorno, CancellationToken ct) =>
        db.PagosPayPal.AsNoTracking().Include(p => p.Reserva).SingleOrDefaultAsync(p =>
            (orden != "" && p.OrdenId == orden || captura != "" && p.CapturaId == captura) && p.Entorno == entorno, ct);
    public void Agregar(PagoPayPal pago)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("El pago requiere transacción y bloqueo del vuelo.");
        db.PagosPayPal.Add(pago);
    }
    public void LimpiarSeguimiento() => db.ChangeTracker.Clear();
    public Task<bool> EventoExisteAsync(string id, CancellationToken ct) => db.EventosPayPal.AnyAsync(e => e.EventoId == id, ct);
    public async Task RegistrarEventoAsync(EventoPayPal evento, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        db.EventosPayPal.Add(evento);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await EventoExisteAsync(evento.EventoId, ct)) throw;
        }
    }
}
