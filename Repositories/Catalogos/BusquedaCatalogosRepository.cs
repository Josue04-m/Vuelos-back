using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Repositories.Catalogos;

/// <summary>Consultas de solo lectura que proyectan pocas columnas; nunca cargan DatosOrigenJson ni otros metadatos pesados.</summary>
public sealed class BusquedaCatalogosRepository(AppDbContext context) : IBusquedaCatalogosRepository
{
    private const int MaxTerminos = 5;
    private const int MaxLongitudTermino = 50;

    private static string[] Terminos(string? q) => (q ?? "")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => t.Length > MaxLongitudTermino ? t[..MaxLongitudTermino] : t)
        .Take(MaxTerminos).ToArray();

    public async Task<IReadOnlyList<AeropuertoResumenResponse>> BuscarAeropuertosAsync(string? q, int limite, CancellationToken ct) =>
        await ConsultaAeropuertos(q, limite).ToListAsync(ct);

    internal IQueryable<AeropuertoResumenResponse> ConsultaAeropuertos(string? q, int limite)
    {
        var terminos = Terminos(q);
        var query = context.Aeropuertos.AsNoTracking().Where(a => a.Activo);
        foreach (var t in terminos)
        {
            var pattern = PatronesPostgreSql.Contiene(t);
            query = query.Where(a => EF.Functions.ILike(a.CodigoIata, pattern, "!") ||
                (a.CodigoIcao != null && EF.Functions.ILike(a.CodigoIcao, pattern, "!")) ||
                EF.Functions.ILike(a.Nombre, pattern, "!") || EF.Functions.ILike(a.Ciudad, pattern, "!") ||
                EF.Functions.ILike(a.Pais, pattern, "!"));
        }
        var primero = (terminos.FirstOrDefault() ?? "").ToUpperInvariant();
        var prefijo = PatronesPostgreSql.Prefijo(primero);
        // Coincidencia exacta de IATA primero, luego prefijo; el resto por nombre.
        return query
            .OrderBy(a => a.CodigoIata.ToUpper() == primero ? 0 : EF.Functions.ILike(a.CodigoIata, prefijo, "!") ? 1 : 2)
            .ThenBy(a => a.Nombre)
            .Take(limite)
            .Select(a => new AeropuertoResumenResponse(a.AeropuertoId, a.CodigoIata, a.CodigoIcao, a.Nombre, a.Ciudad, a.Pais));
    }

    public Task<AeropuertoResumenResponse?> ObtenerAeropuertoAsync(int id, CancellationToken ct) =>
        context.Aeropuertos.AsNoTracking().Where(a => a.AeropuertoId == id)
            .Select(a => new AeropuertoResumenResponse(a.AeropuertoId, a.CodigoIata, a.CodigoIcao, a.Nombre, a.Ciudad, a.Pais))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AvionResumenResponse>> BuscarAvionesAsync(string? q, int limite, CancellationToken ct) =>
        await ConsultaAviones(q, limite).ToListAsync(ct);

    internal IQueryable<AvionResumenResponse> ConsultaAviones(string? q, int limite)
    {
        var terminos = Terminos(q);
        var query = context.Aviones.AsNoTracking().Where(a => a.Activo);
        foreach (var t in terminos)
        {
            var pattern = PatronesPostgreSql.Contiene(t);
            query = query.Where(a => EF.Functions.ILike(a.Modelo, pattern, "!") || EF.Functions.ILike(a.Matricula, pattern, "!") ||
                (a.CodigoIata != null && EF.Functions.ILike(a.CodigoIata, pattern, "!")) ||
                (a.CodigoIcao != null && EF.Functions.ILike(a.CodigoIcao, pattern, "!")));
        }
        return query.OrderBy(a => a.Modelo).ThenBy(a => a.Matricula)
            .Take(limite)
            .Select(a => new AvionResumenResponse(a.AvionId, a.Modelo, a.Matricula, a.CapacidadTotal));
    }
}
