using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;
namespace Vuelos_Backend.Repositories.Reservas;

public sealed class ReservaRepository(AppDbContext context) : IReservaRepository
{
    public async Task<ITransaccionReserva> IniciarTransaccionAsync(CancellationToken cancellationToken) =>
        new Transaccion(context,
            await EjecutarAsync(() => context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)));

    public Task<Vuelo?> BloquearVueloAsync(int vueloId, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        // Todas las escrituras bloquean primero el vuelo. HOLDLOCK conserva el
        // bloqueo hasta el commit y también impide cambios administrativos de estado.
        return EjecutarAsync(() => ConsultaVueloBloqueado(vueloId).SingleOrDefaultAsync(cancellationToken));
    }

    public Task<Reserva?> BloquearPropiaAsync(int reservaId, int usuarioId, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => ConsultaReservaBloqueada(reservaId, usuarioId).SingleOrDefaultAsync(cancellationToken));
    }

    internal IQueryable<Vuelo> ConsultaVueloBloqueado(int vueloId) =>
        context.Vuelos.FromSqlInterpolated(
            $"SELECT * FROM [dbo].[vuelos] WITH (UPDLOCK, HOLDLOCK) WHERE [vuelo_id] = {vueloId}")
            .Include(v => v.Avion);

    internal IQueryable<Reserva> ConsultaReservaBloqueada(int reservaId, int usuarioId) =>
        context.Reservas.FromSqlInterpolated(
            $"SELECT * FROM [dbo].[reservas] WITH (UPDLOCK, HOLDLOCK) WHERE [reserva_id] = {reservaId} AND [usuario_id] = {usuarioId}");

    public Task<int?> ObtenerVueloPropioAsync(int reservaId, int usuarioId, CancellationToken cancellationToken) =>
        EjecutarAsync(() => context.Reservas.AsNoTracking()
            .Where(r => r.ReservaId == reservaId && r.UsuarioId == usuarioId)
            .Select(r => (int?)r.VueloId).SingleOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<AsientoVuelo>> ObtenerAsientosAsync(
        int vueloId, int[] asientos, CancellationToken cancellationToken) =>
        await EjecutarAsync(() => context.AsientosVuelo.AsNoTracking()
            .Where(a => a.VueloId == vueloId && a.AsientoFisico.AvionId == a.Vuelo.AvionId && asientos.Contains(a.AsientoVueloId))
            .Include(a => a.AsientoFisico).OrderBy(a => a.AsientoVueloId).ToListAsync(cancellationToken));

    public async Task<IReadOnlyList<DetalleReserva>> ObtenerActivosAsync(
        int reservaId, CancellationToken cancellationToken) =>
        await EjecutarAsync(() => context.DetallesReserva.Where(d => d.ReservaId == reservaId && d.Estado == "activo")
            .OrderBy(d => d.DetalleId).ToListAsync(cancellationToken));

    public Task<int> CambiarEstadoAsientoAsync(int asientoId, int vueloId,
        string estadoAnterior, string estadoNuevo, byte[] rowVersion, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => ConsultaAsientoVersionado(asientoId, vueloId, estadoAnterior, rowVersion)
            .ExecuteUpdateAsync(update => update.SetProperty(a => a.Estado, estadoNuevo), cancellationToken));
    }

    internal IQueryable<AsientoVuelo> ConsultaAsientoVersionado(int asientoId, int vueloId, string estado, byte[] version) =>
        context.AsientosVuelo.Where(a => a.AsientoVueloId == asientoId && a.VueloId == vueloId && a.AsientoFisico.AvionId == a.Vuelo.AvionId &&
            a.Estado == estado && a.RowVersion == version);

    public void AgregarReserva(Reserva reserva) => context.Reservas.Add(reserva);
    public void AgregarDetalle(DetalleReserva detalle) => context.DetallesReserva.Add(detalle);
    public void AgregarHistorial(HistorialCambioReserva historial) => context.HistorialCambiosReserva.Add(historial);

    public void PrepararVersionReserva(Reserva reserva, byte[] version)
    {
        context.Entry(reserva).Property(r => r.RowVersion).OriginalValue = version;
        // Fuerza UPDATE aunque el estado ya sea modificada y el importe no cambie.
        context.Entry(reserva).Property(r => r.Estado).IsModified = true;
    }

    public Task GuardarAsync(CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => context.SaveChangesAsync(cancellationToken));
    }

    public async Task<ReservaLectura?> ObtenerPropiaAsync(int reservaId, int usuarioId, CancellationToken cancellationToken)
    {
        // No incluir Usuario evita ocultar historial por el filtro lógico de ese usuario.
        var reserva = await EjecutarAsync(() => context.Reservas.AsNoTracking().Include(r => r.Vuelo)
            .SingleOrDefaultAsync(r => r.ReservaId == reservaId && r.UsuarioId == usuarioId, cancellationToken));
        if (reserva is null) return null;
        var detalles = await EjecutarAsync(() => context.DetallesReserva.AsNoTracking()
            .Where(d => d.ReservaId == reservaId).Include(d => d.AsientoVuelo).ThenInclude(a => a.AsientoFisico)
            .OrderBy(d => d.DetalleId).ToListAsync(cancellationToken));
        var historial = await EjecutarAsync(() => context.HistorialCambiosReserva.AsNoTracking()
            .Where(h => h.ReservaId == reservaId).OrderBy(h => h.FechaAccion).ThenBy(h => h.HistorialReservaId)
            .ToListAsync(cancellationToken));
        return new ReservaLectura(reserva, detalles, historial);
    }

    public async Task<ResultadoBusquedaReservas> BuscarPropiasAsync(
        int usuarioId, FiltroReservas filtro, CancellationToken cancellationToken)
    {
        var query = context.Reservas.AsNoTracking().Where(r => r.UsuarioId == usuarioId);
        if (filtro.Estado is not null) query = query.Where(r => r.Estado == filtro.Estado);
        if (filtro.DesdeUtc is { } desde) query = query.Where(r => r.FechaReserva >= desde);
        if (filtro.HastaUtcExclusiva is { } hasta) query = query.Where(r => r.FechaReserva < hasta);
        var total = await EjecutarAsync(() => query.CountAsync(cancellationToken));
        var reservas = await EjecutarAsync(() => query.Include(r => r.Vuelo)
            .OrderByDescending(r => r.FechaReserva).ThenByDescending(r => r.ReservaId)
            .Skip(checked((filtro.Pagina - 1) * filtro.TamanoPagina)).Take(filtro.TamanoPagina).ToListAsync(cancellationToken));
        return new ResultadoBusquedaReservas(reservas, total);
    }

    private void ExigirTransaccion()
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La escritura de reservas requiere una transacción explícita.");
    }

    private static async Task<T> EjecutarAsync<T>(Func<Task<T>> accion)
    {
        try { return await accion(); }
        catch (Exception exception) when (TraducirConflicto(exception) is not null)
        {
            throw TraducirConflicto(exception)!;
        }
    }

    private static ReservaOperacionException? TraducirConflicto(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return new(ErrorReserva.Conflicto, "version_desactualizada", "La información cambió. Consulte la versión vigente.");
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        if (sql is null) return null;
        if (sql.Number == 1205)
            return new(ErrorReserva.Conflicto, "operacion_concurrente", "La operación compitió con otra escritura. Consulte el estado actual.");
        if (sql.Errors.Cast<SqlError>().Any(e => e.Number is 2601 or 2627 &&
            e.Message.Contains("UX_detalle_reservas_asiento_activo", StringComparison.OrdinalIgnoreCase)))
            return new(ErrorReserva.Conflicto, "asiento_no_disponible", "Un asiento ya tiene una asignación activa.");
        if (sql.Errors.Cast<SqlError>().Any(e => e.Number is 2601 or 2627 &&
            e.Message.Contains("UQ_reservas_codigo_reserva", StringComparison.OrdinalIgnoreCase)))
            return new(ErrorReserva.Conflicto, "codigo_reserva_ocupado", "No se pudo asignar un código único a la reserva.");
        return null;
    }

    private sealed class Transaccion(AppDbContext context, IDbContextTransaction transaction) : ITransaccionReserva
    {
        private bool confirmada;
        public async Task ConfirmarAsync(CancellationToken cancellationToken)
        {
            await EjecutarAsync(async () => { await transaction.CommitAsync(cancellationToken); return true; });
            confirmada = true;
        }
        public async ValueTask DisposeAsync()
        {
            try { await transaction.DisposeAsync(); } // Sin commit, SQL Server revierte todo.
            finally { if (!confirmada) context.ChangeTracker.Clear(); }
        }
    }
}
