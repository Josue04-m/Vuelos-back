using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Vuelos;

namespace Vuelos_Backend.Repositories.Vuelos;

public sealed class VueloAdminRepository(AppDbContext context) : IVueloAdminRepository
{
    public async Task<ITransaccionVueloAdmin> IniciarTransaccionAsync(CancellationToken cancellationToken) => await EjecutarAsync(async () =>
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await BloqueoAdministrativo.AdquirirAsync(context, cancellationToken);
            return (ITransaccionVueloAdmin)new Transaccion(context, transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    });

    // Serializa altas y cambios de programación del mismo avión, incluso cuando aún no hay vuelos.
    public Task<Avion?> BloquearAvionAsync(int avionId, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => ConsultasBloqueoPostgreSql.Avion(context, avionId)
            .SingleOrDefaultAsync(cancellationToken));
    }

    public Task<int?> ObtenerAvionIdAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(() => context.Vuelos.AsNoTracking().Where(v => v.VueloId == vueloId)
            .Select(v => (int?)v.AvionId).SingleOrDefaultAsync(cancellationToken));

    public Task<Vuelo?> ObtenerAsync(int vueloId, bool bloquear, CancellationToken cancellationToken)
    {
        if (bloquear) ExigirTransaccion();
        var query = bloquear
            ? ConsultasBloqueoPostgreSql.Vuelo(context, vueloId)
            : context.Vuelos.AsNoTracking().Where(v => v.VueloId == vueloId);
        return EjecutarAsync(() => query.Include(v => v.Avion).Include(v => v.AeropuertoOrigen)
            .Include(v => v.AeropuertoDestino).SingleOrDefaultAsync(cancellationToken));
    }

    public Task<bool> ExisteAeropuertoAsync(int aeropuertoId, CancellationToken cancellationToken) =>
        EjecutarAsync(() => context.Aeropuertos.AnyAsync(a => a.AeropuertoId == aeropuertoId && a.Activo, cancellationToken));

    public async Task<IReadOnlyList<AsientoFisico>> ObtenerAsientosFisicosAsync(int avionId, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return await EjecutarAsync(() => ConsultasBloqueoPostgreSql.AsientosFisicos(context, avionId)
            .AsNoTracking().Where(a => a.Activo && a.Categoria.Activo).OrderBy(a => a.AsientoFisicoId).ToListAsync(cancellationToken));
    }

    public Task<bool> TieneReservasVigentesAsync(int vueloId, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        // No incluir Usuario: el borrado lógico de una cuenta no elimina sus reservas.
        return EjecutarAsync(async () =>
            await context.Reservas.AnyAsync(r => r.VueloId == vueloId && r.Estado != "cancelada", cancellationToken) ||
            await context.DetallesReserva.AnyAsync(d => d.AsientoVuelo.VueloId == vueloId && d.Estado == "activo", cancellationToken));
    }

    public Task<bool> TieneSolapamientoAsync(int avionId, int? excluirVueloId, DateTime salidaUtc,
        DateTime llegadaUtc, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => context.Vuelos.AnyAsync(v => v.AvionId == avionId &&
            (!excluirVueloId.HasValue || v.VueloId != excluirVueloId.Value) && v.Estado != "cancelado" &&
            v.SalidaProgramada < llegadaUtc && v.LlegadaProgramada > salidaUtc, cancellationToken));
    }

    public Task<int> ContarAsientosAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(() => context.AsientosVuelo.CountAsync(a => a.VueloId == vueloId && a.AsientoFisico.AvionId == a.Vuelo.AvionId, cancellationToken));

    public Task<bool> TieneReservasRegistradasAsync(int vueloId, CancellationToken ct)
    {
        ExigirTransaccion();
        return EjecutarAsync(async () => await context.Reservas.AnyAsync(r => r.VueloId == vueloId &&
            (r.FechaExpiracion == null || r.Estado == "pendiente_pago" || (context.ComprasSimuladas.Any(c => c.ReservaId == r.ReservaId) || context.PagosPayPal.Any(p => p.ReservaId == r.ReservaId && (p.FechaCaptura != null || p.BloqueaReserva)))), ct) ||
            await context.DetallesReserva.AnyAsync(d => d.AsientoVuelo.VueloId == vueloId && d.Estado == "activo", ct));
    }

    public Task ExpirarPendientesAsync(int vueloId, DateTime ahoraUtc, bool forzar, CancellationToken ct) =>
        EjecutarAsync(async () => {
            if (forzar && await context.PagosPayPal.AnyAsync(p => p.Reserva.VueloId == vueloId && p.BloqueaReserva, ct))
                throw new VueloAdminOperacionException(ErrorVueloAdmin.Conflicto, "pago_en_curso", "Concilie los pagos PayPal pendientes antes de cerrar el vuelo.");
            await Reservas.CicloReservaRepository.ExpirarEnContextoAsync(context, vueloId, ahoraUtc, forzar, ct); return true;
        });

    public async Task<IReadOnlyList<AsientoVuelo>> ObtenerTodosAsientosAsync(int vueloId, CancellationToken ct)
    {
        ExigirTransaccion();
        return await EjecutarAsync(() => context.AsientosVuelo.Where(a => a.VueloId == vueloId).ToListAsync(ct));
    }

    public void AgregarAsientos(IReadOnlyList<AsientoVuelo> asientos)
    {
        ExigirTransaccion();
        context.AsientosVuelo.AddRange(asientos);
    }

    public void Agregar(Vuelo vuelo, IReadOnlyList<AsientoVuelo> asientos, HistorialEstadoVuelo historial)
    {
        ExigirTransaccion();
        context.Vuelos.Add(vuelo);
        context.AsientosVuelo.AddRange(asientos);
        context.HistorialEstadosVuelo.Add(historial);
    }

    public void AgregarHistorial(HistorialEstadoVuelo historial)
    {
        ExigirTransaccion();
        context.HistorialEstadosVuelo.Add(historial);
    }

    public async Task CancelarReservasYLiberarAsientosAsync(int vueloId, DateTime ahoraUtc, CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        if (await context.PagosPayPal.AnyAsync(p => p.Reserva.VueloId == vueloId && (p.BloqueaReserva || p.FechaCaptura != null), cancellationToken))
            throw new Vuelos_Backend.Models.Vuelos.VueloAdminOperacionException(Vuelos_Backend.Models.Vuelos.ErrorVueloAdmin.Conflicto,
                "pagos_requieren_revision", "El vuelo tiene pagos PayPal. Requiere resolverlos antes de cancelar el vuelo.");
        var reservas = await context.Reservas
            .Where(r => r.VueloId == vueloId && r.Estado != "cancelada" && r.Estado != "expirada")
            .ToListAsync(cancellationToken);

        foreach (var reserva in reservas)
        {
            reserva.Estado = "cancelada";
            context.HistorialCambiosReserva.Add(new HistorialCambioReserva
            {
                Reserva = reserva,
                TipoAccion = "CANCELACION",
                FechaAccion = ahoraUtc,
                Observacion = "Cancelación por cancelación del vuelo."
            });
        }

        var detallesActivos = await context.DetallesReserva
            .Where(d => d.AsientoVuelo.VueloId == vueloId && d.Estado == "activo")
            .ToListAsync(cancellationToken);

        foreach (var detalle in detallesActivos)
        {
            detalle.Estado = "liberado";
        }

        var asientosReservados = await context.AsientosVuelo
            .Where(a => a.VueloId == vueloId && a.Estado == "reservado")
            .ToListAsync(cancellationToken);

        foreach (var asiento in asientosReservados)
        {
            asiento.Estado = "disponible";
        }
    }

    public Task<IReadOnlyList<HistorialEstadoVuelo>> ObtenerHistorialAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(async () => (IReadOnlyList<HistorialEstadoVuelo>)await ConsultaHistorial(vueloId).ToListAsync(cancellationToken));

    internal IQueryable<HistorialEstadoVuelo> ConsultaHistorial(int vueloId) =>
        context.HistorialEstadosVuelo
            // La eliminación lógica del actor no debe ocultar eventos históricos.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(h => h.VueloId == vueloId)
            .Include(h => h.UsuarioAdmin)
            .OrderByDescending(h => h.FechaCambio)
            .ThenByDescending(h => h.HistorialId);

    public Task<IReadOnlyList<AsientoVuelo>> ListarAsientosConDetallesAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(async () => (IReadOnlyList<AsientoVuelo>)await context.AsientosVuelo
            .AsNoTracking()
            .Where(a => a.VueloId == vueloId && a.AsientoFisico.AvionId == a.Vuelo.AvionId)
            .Include(a => a.AsientoFisico)
            .ThenInclude(f => f.Categoria)
            .OrderBy(a => a.AsientoFisico.CodigoAsiento)
            .ToListAsync(cancellationToken));

    public Task<HashSet<int>> ObtenerIdsAsientosConReservaActivaAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(async () =>
        {
            var ids = await context.DetallesReserva
                .AsNoTracking()
                .Where(d => d.AsientoVuelo.VueloId == vueloId && d.Estado == "activo")
                .Select(d => d.AsientoVueloId)
                .Distinct()
                .ToListAsync(cancellationToken);
            return ids.ToHashSet();
        });

    public Task<AsientoVuelo?> ObtenerAsientoVueloAsync(int vueloId, int asientoVueloId, bool bloquear, CancellationToken cancellationToken)
    {
        if (bloquear) ExigirTransaccion();
        var query = bloquear
            ? ConsultasBloqueoPostgreSql.AsientoVuelo(context, asientoVueloId, vueloId)
            : context.AsientosVuelo.AsNoTracking().Where(a => a.AsientoVueloId == asientoVueloId && a.VueloId == vueloId);
        return EjecutarAsync(() => query.Where(a => a.AsientoFisico.AvionId == a.Vuelo.AvionId)
            .Include(a => a.AsientoFisico)
            .ThenInclude(f => f.Categoria)
            .SingleOrDefaultAsync(cancellationToken));
    }

    public Task<bool> AsientoTieneReservaActivaAsync(int asientoVueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(() => context.DetallesReserva.AnyAsync(
            d => d.AsientoVueloId == asientoVueloId && d.Estado == "activo", cancellationToken));

    public Task<IReadOnlyList<Reserva>> ObtenerReservasDeVueloAsync(int vueloId, CancellationToken cancellationToken) =>
        EjecutarAsync(async () => (IReadOnlyList<Reserva>)await context.Reservas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.VueloId == vueloId)
            .Include(r => r.Usuario)
            .OrderByDescending(r => r.FechaReserva)
            .ThenByDescending(r => r.ReservaId)
            .ToListAsync(cancellationToken));

    public Task<IReadOnlyList<DetalleReserva>> ObtenerDetallesDeReservasAsync(IEnumerable<int> reservaIds, CancellationToken cancellationToken) =>
        EjecutarAsync(async () =>
        {
            var idList = reservaIds.ToList();
            if (idList.Count == 0) return (IReadOnlyList<DetalleReserva>)[];
            return (IReadOnlyList<DetalleReserva>)await context.DetallesReserva
                .AsNoTracking()
                .Where(d => idList.Contains(d.ReservaId))
                .Include(d => d.AsientoVuelo)
                .ThenInclude(a => a.AsientoFisico)
                .OrderBy(d => d.DetalleId)
                .ToListAsync(cancellationToken);
        });

    public Task GuardarAsync(CancellationToken cancellationToken)
    {
        ExigirTransaccion();
        return EjecutarAsync(() => context.SaveChangesAsync(cancellationToken));
    }

    private void ExigirTransaccion()
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La escritura administrativa requiere una transacción explícita.");
    }

    private static async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion)
    {
        try { return await operacion(); }
        catch (DbUpdateConcurrencyException)
        {
            throw Conflicto("La información cambió. Consulte el vuelo o asiento e intente nuevamente.");
        }
        catch (Exception ex) when (ErroresPostgreSql.EsConflicto(ex))
        {
            throw Conflicto("La operación compite con otro cambio o sus referencias ya no están disponibles. Consulte los datos actuales.");
        }
    }

    private static VueloAdminOperacionException Conflicto(string mensaje) =>
        new(ErrorVueloAdmin.Conflicto, "conflicto_vuelo", mensaje);

    private sealed class Transaccion(AppDbContext context, IDbContextTransaction transaction) : ITransaccionVueloAdmin
    {
        private bool confirmada;
        public async Task ConfirmarAsync(CancellationToken cancellationToken)
        {
            await EjecutarAsync(async () => { await transaction.CommitAsync(cancellationToken); return true; });
            confirmada = true;
        }

        public async ValueTask DisposeAsync()
        {
            try { await transaction.DisposeAsync(); }
            finally { if (!confirmada) context.ChangeTracker.Clear(); }
        }
    }
}
