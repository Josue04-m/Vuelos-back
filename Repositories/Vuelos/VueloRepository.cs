using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Vuelos;
namespace Vuelos_Backend.Repositories.Vuelos;

public sealed class VueloRepository(AppDbContext context) : IVueloRepository
{
    // Separado para verificar la traducción SQL sin abrir una conexión.
    internal IQueryable<Vuelo> AplicarFiltros(FiltroVuelos filtro)
    {
        var query = context.Vuelos.AsNoTracking();
        if (filtro.Origen is not null)
            query = query.Where(v => v.AeropuertoOrigen.CodigoIata == filtro.Origen);
        if (filtro.Destino is not null)
            query = query.Where(v => v.AeropuertoDestino.CodigoIata == filtro.Destino);
        if (filtro.SalidaDesdeUtc is { } desde)
            query = query.Where(v => v.SalidaProgramada >= desde);
        if (filtro.SalidaHastaUtcExclusiva is { } hasta)
            query = query.Where(v => v.SalidaProgramada < hasta);
        return query;
    }

    public async Task<ResultadoBusquedaVuelos> BuscarAsync(FiltroVuelos filtro, CancellationToken cancellationToken)
    {
        var query = AplicarFiltros(filtro);
        var total = await query.CountAsync(cancellationToken);
        var vuelos = await ConDetalle(query).OrderBy(v => v.SalidaProgramada).ThenBy(v => v.VueloId)
            .Skip(checked((filtro.Pagina - 1) * filtro.TamanoPagina)).Take(filtro.TamanoPagina)
            .ToListAsync(cancellationToken);
        return new ResultadoBusquedaVuelos(vuelos, total);
    }

    public Task<Vuelo?> ObtenerAsync(int vueloId, CancellationToken cancellationToken) =>
        ConDetalle(context.Vuelos.AsNoTracking())
            .SingleOrDefaultAsync(v => v.VueloId == vueloId, cancellationToken);

    public async Task<IReadOnlyList<AsientoVuelo>> ListarAsientosAsync(int vueloId, CancellationToken cancellationToken) =>
        await context.AsientosVuelo.AsNoTracking().Where(a => a.VueloId == vueloId && a.AsientoFisico.AvionId == a.Vuelo.AvionId)
            .Include(a => a.AsientoFisico).ThenInclude(a => a.Categoria)
            .OrderBy(a => a.AsientoFisico.CodigoAsiento).ThenBy(a => a.AsientoVueloId)
            .ToListAsync(cancellationToken);

    private static IQueryable<Vuelo> ConDetalle(IQueryable<Vuelo> query) =>
        query.Include(v => v.AeropuertoOrigen).Include(v => v.AeropuertoDestino).Include(v => v.Avion);
}
