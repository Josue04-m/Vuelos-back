using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Models.Common;

namespace Vuelos_Backend.Repositories.Catalogos;

public sealed class CatalogosAdminRepository(AppDbContext context) : ICatalogosAdminRepository
{
    public async Task<ITransaccionCatalogo> IniciarTransaccionAsync(CancellationToken ct) => await Ejecutar(async () =>
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await BloqueoAdministrativo.AdquirirAsync(context, ct);
            return (ITransaccionCatalogo)new Transaccion(context, transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    });

    internal IQueryable<T> Consulta<T>(FiltroCatalogoRequest filtro) where T : class, ICatalogoAdministrable
    {
        var query = context.Set<T>().AsNoTracking();
        if (!filtro.IncluirInactivos) query = query.Where(e => e.Activo == (filtro.Activo ?? true));
        if (typeof(T) == typeof(AsientoFisico) && filtro.AvionId is int avionId)
            query = query.Where(e => EF.Property<int>(e, "AvionId") == avionId);
        var buscar = filtro.Buscar?.Trim();
        if (!string.IsNullOrEmpty(buscar))
        {
            var campo = typeof(T) == typeof(AsientoFisico) ? "CodigoAsiento" : typeof(T) == typeof(Avion) ? "Modelo" : "Nombre";
            var alterno = typeof(T) == typeof(Aeropuerto) ? "CodigoIata" : typeof(T) == typeof(Avion) ? "Matricula" : campo;
            query = query.Where(e => EF.Property<string>(e, campo).Contains(buscar) || EF.Property<string>(e, alterno).Contains(buscar));
        }
        return query.OrderBy(e => EF.Property<int>(e, Clave<T>()));
    }

    public Task<(IReadOnlyList<T> Elementos, int Total)> BuscarAsync<T>(FiltroCatalogoRequest filtro, CancellationToken ct)
        where T : class, ICatalogoAdministrable => Ejecutar(async () =>
        {
            var query = Consulta<T>(filtro);
            var total = await query.CountAsync(ct);
            var rows = await query.Skip(checked((filtro.Pagina - 1) * filtro.TamanoPagina)).Take(filtro.TamanoPagina).ToListAsync(ct);
            return ((IReadOnlyList<T>)rows, total);
        });

    public Task<T?> ObtenerAsync<T>(int id, CancellationToken ct) where T : class, ICatalogoAdministrable =>
        Ejecutar(() => context.Set<T>().AsNoTracking().SingleOrDefaultAsync(e => EF.Property<int>(e, Clave<T>()) == id, ct));

    public async Task CrearAsync<T>(T entity, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        ExigirTransaccion();
        context.Set<T>().Add(entity);
        await Ejecutar(() => context.SaveChangesAsync(ct));
    }

    public async Task CrearAsientosAsync(IReadOnlyList<AsientoFisico> asientos, CancellationToken ct)
    {
        ExigirTransaccion();
        context.AsientosFisicos.AddRange(asientos);
        await Ejecutar(() => context.SaveChangesAsync(ct));
    }

    public async Task<IReadOnlyList<Avion>> ObtenerPorMatriculasAsync(string[] matriculas, CancellationToken ct) =>
        await Ejecutar(() => context.Aviones.AsNoTracking().Where(a => matriculas.Contains(a.Matricula)).ToListAsync(ct));

    public void LimpiarSeguimientoImportacion()
    {
        ExigirTransaccion();
        // Lo guardado sigue dentro de la transacción; limitar el seguimiento al avión que se está creando.
        context.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<Aeropuerto>> ObtenerPorCodigosIataAsync(string[] codigos, CancellationToken ct)
    {
        var resultado = new List<Aeropuerto>();
        foreach (var grupo in codigos.Chunk(500))
            resultado.AddRange(await Ejecutar(() => context.Aeropuertos.AsNoTracking().Where(a => grupo.Contains(a.CodigoIata)).ToListAsync(ct)));
        return resultado;
    }

    public async Task CrearAeropuertosAsync(IReadOnlyList<Aeropuerto> aeropuertos, CancellationToken ct)
    {
        ExigirTransaccion();
        foreach (var lote in aeropuertos.Chunk(200))
        {
            context.Aeropuertos.AddRange(lote);
            try { await Ejecutar(() => context.SaveChangesAsync(ct)); }
            catch (Exception ex) when ((ex as SqlException ?? ex.InnerException as SqlException)?.Number is 2628 or 8152)
            { throw new CatalogoOperacionException(ErrorCatalogo.SolicitudInvalida, "longitud_aeropuerto_incompatible", "Un campo excede los bytes admitidos por SQL Server. No se guardó la importación; revise nombres, ciudades y codificación."); }
            // Todos los lotes permanecen en la misma transacción; limitar memoria de seguimiento.
            context.ChangeTracker.Clear();
            var ids = lote.Select(a => a.AeropuertoId).ToArray();
            var leidos = await Ejecutar(() => context.Aeropuertos.AsNoTracking().Where(a => ids.Contains(a.AeropuertoId)).ToListAsync(ct));
            ExigirDatosAeropuertosConservados(lote, leidos);
        }
    }

    internal static void ExigirDatosAeropuertosConservados(IReadOnlyList<Aeropuerto> esperados, IReadOnlyList<Aeropuerto> leidos)
    {
        var porId = leidos.ToDictionary(a => a.AeropuertoId);
        foreach (var a in esperados)
            if (!porId.TryGetValue(a.AeropuertoId, out var b) || a.CodigoIata != b.CodigoIata || a.Nombre != b.Nombre || a.Ciudad != b.Ciudad || a.Pais != b.Pais ||
                a.IdOrigen != b.IdOrigen || a.Identificador != b.Identificador || a.Tipo != b.Tipo || a.Latitud != b.Latitud || a.Longitud != b.Longitud ||
                a.ElevacionPies != b.ElevacionPies || a.Continente != b.Continente || a.RegionIso != b.RegionIso || a.ServicioRegular != b.ServicioRegular ||
                a.CodigoIcao != b.CodigoIcao || a.CodigoGps != b.CodigoGps || a.CodigoLocal != b.CodigoLocal || a.SitioWeb != b.SitioWeb ||
                a.Wikipedia != b.Wikipedia || a.PalabrasClave != b.PalabrasClave || a.DatosOrigenJson != b.DatosOrigenJson)
                throw new CatalogoOperacionException(ErrorCatalogo.SolicitudInvalida, "codificacion_aeropuerto_incompatible",
                    $"SQL Server no conservó los datos del aeropuerto {a.CodigoIata}. Se revierte toda la carga. Revise los tipos y la codificación antes de importar ese registro.");
    }

    // Las claves únicas originales son alternate keys de EF y no son editables mediante seguimiento.
    // UPDATE parametrizado conserva esas restricciones SQL y comprueba RowVersion en la escritura.
    public async Task ActualizarAsync<T>(T entity, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        ExigirTransaccion();
        var affected = await Ejecutar(() => entity switch
        {
            Aeropuerto a => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [dbo].[aeropuertos] SET [codigo_iata]={a.CodigoIata}, [nombre]={a.Nombre}, [ciudad]={a.Ciudad}, [pais]={a.Pais} WHERE [aeropuerto_id]={a.AeropuertoId} AND [row_version]={a.RowVersion}", ct),
            Avion a => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [dbo].[aviones] SET [modelo]={a.Modelo}, [matricula]={a.Matricula}, [capacidad_total]={a.CapacidadTotal} WHERE [avion_id]={a.AvionId} AND [row_version]={a.RowVersion}", ct),
            CategoriaAsiento a => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [dbo].[categorias_asiento] SET [nombre]={a.Nombre}, [descripcion]={a.Descripcion} WHERE [categoria_id]={a.CategoriaId} AND [row_version]={a.RowVersion}", ct),
            AsientoFisico a => context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [dbo].[asientos_fisicos] SET [avion_id]={a.AvionId}, [categoria_id]={a.CategoriaId}, [codigo_asiento]={a.CodigoAsiento} WHERE [asiento_fisico_id]={a.AsientoFisicoId} AND [row_version]={a.RowVersion}", ct),
            _ => throw new InvalidOperationException("Catálogo no soportado.")
        });
        if (affected != 1) throw Conflicto("La versión cambió. Consulte nuevamente el elemento.");
    }

    public async Task DesactivarAsync<T>(int id, byte[] version, DateTime ahoraUtc, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        ExigirTransaccion();
        var affected = await Ejecutar(() => context.Set<T>().Where(e => EF.Property<int>(e, Clave<T>()) == id && e.RowVersion == version)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.Activo, false).SetProperty(e => e.FechaDesactivacion, (DateTime?)ahoraUtc), ct));
        if (affected != 1) throw Conflicto("La versión cambió. Consulte nuevamente el elemento.");
    }

    public Task<bool> TieneVuelosAsync(TipoCatalogo tipo, int id, bool soloVigentes, CancellationToken ct) => Ejecutar(() =>
    {
        var vuelos = context.Vuelos.AsNoTracking();
        if (soloVigentes) vuelos = vuelos.Where(v => v.Estado != "cancelado" && v.Estado != "finalizado");
        vuelos = tipo switch
        {
            TipoCatalogo.Aeropuertos => vuelos.Where(v => v.AeropuertoOrigenId == id || v.AeropuertoDestinoId == id),
            TipoCatalogo.Aviones => vuelos.Where(v => v.AvionId == id),
            TipoCatalogo.Categorias => vuelos.Where(v => context.AsientosVuelo.Any(a => a.VueloId == v.VueloId && a.AsientoFisico.CategoriaId == id)),
            TipoCatalogo.AsientosFisicos => vuelos.Where(v => context.AsientosVuelo.Any(a => a.VueloId == v.VueloId && a.AsientoFisicoId == id)),
            _ => throw new InvalidOperationException("Catálogo no soportado.")
        };
        return vuelos.AnyAsync(ct);
    });

    public Task<int> ContarAsientosActivosAsync(int avionId, int? excluirId, CancellationToken ct) => Ejecutar(() =>
        context.AsientosFisicos.CountAsync(a => a.AvionId == avionId && a.Activo && (!excluirId.HasValue || a.AsientoFisicoId != excluirId.Value), ct));
    public Task<bool> CategoriaTieneAsientosActivosAsync(int categoriaId, CancellationToken ct) => Ejecutar(() =>
        context.AsientosFisicos.AnyAsync(a => a.CategoriaId == categoriaId && a.Activo, ct));
    public Task<bool> AsientoFueAsignadoAsync(int asientoId, CancellationToken ct) => Ejecutar(() =>
        context.AsientosVuelo.AnyAsync(a => a.AsientoFisicoId == asientoId, ct));

    private static string Clave<T>() => typeof(T) == typeof(Aeropuerto) ? "AeropuertoId" : typeof(T) == typeof(Avion) ? "AvionId"
        : typeof(T) == typeof(CategoriaAsiento) ? "CategoriaId" : typeof(T) == typeof(AsientoFisico) ? "AsientoFisicoId"
        : throw new InvalidOperationException("Catálogo no soportado.");

    private void ExigirTransaccion()
    {
        if (context.Database.CurrentTransaction is null) throw new InvalidOperationException("Se requiere transacción administrativa.");
    }
    private static CatalogoOperacionException Conflicto(string message) => new(ErrorCatalogo.Conflicto, "conflicto_catalogo", message);
    private static async Task<T> Ejecutar<T>(Func<Task<T>> accion)
    {
        try { return await accion(); }
        catch (DbUpdateConcurrencyException) { throw Conflicto("La información cambió. Consulte la versión vigente."); }
        catch (Exception ex) when ((ex as SqlException ?? ex.InnerException as SqlException)?.Number is 2601 or 2627)
        { throw Conflicto("Ya existe un elemento con ese código, matrícula o nombre, incluso si está inactivo."); }
        catch (Exception ex) when ((ex as SqlException ?? ex.InnerException as SqlException)?.Number is 1205 or 1222 or 51001 or 547)
        { throw Conflicto("Otra operación modificó los datos o sus dependencias. Consulte nuevamente."); }
    }

    private sealed class Transaccion(AppDbContext context, IDbContextTransaction transaction) : ITransaccionCatalogo
    {
        private bool confirmada;
        public async Task ConfirmarAsync(CancellationToken ct)
        {
            await Ejecutar(async () => { await transaction.CommitAsync(ct); return true; });
            confirmada = true;
        }
        public async ValueTask DisposeAsync()
        {
            try { await transaction.DisposeAsync(); }
            finally { if (!confirmada) context.ChangeTracker.Clear(); }
        }
    }
}
