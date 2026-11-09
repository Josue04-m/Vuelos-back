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

    public async Task<IReadOnlyList<AeropuertoResumenResponse>> BuscarAeropuertosAsync(string? q, int limite, CancellationToken ct)
    {
        var terminos = Terminos(q);
        var query = context.Aeropuertos.AsNoTracking().Where(a => a.Activo);
        foreach (var t in terminos)
        {
            var termino = t;
            query = query.Where(a => a.CodigoIata.Contains(termino) || (a.CodigoIcao != null && a.CodigoIcao.Contains(termino)) ||
                a.Nombre.Contains(termino) || a.Ciudad.Contains(termino) || a.Pais.Contains(termino));
        }
        var primero = terminos.FirstOrDefault() ?? "";
        // Coincidencia exacta de IATA primero, luego prefijo; el resto por nombre.
        return await query
            .OrderBy(a => a.CodigoIata == primero ? 0 : a.CodigoIata.StartsWith(primero) ? 1 : 2)
            .ThenBy(a => a.Nombre)
            .Take(limite)
            .Select(a => new AeropuertoResumenResponse(a.AeropuertoId, a.CodigoIata, a.CodigoIcao, a.Nombre, a.Ciudad, a.Pais))
            .ToListAsync(ct);
    }

    public Task<AeropuertoResumenResponse?> ObtenerAeropuertoAsync(int id, CancellationToken ct) =>
        context.Aeropuertos.AsNoTracking().Where(a => a.AeropuertoId == id)
            .Select(a => new AeropuertoResumenResponse(a.AeropuertoId, a.CodigoIata, a.CodigoIcao, a.Nombre, a.Ciudad, a.Pais))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AvionResumenResponse>> BuscarAvionesAsync(string? q, int limite, CancellationToken ct)
    {
        var terminos = Terminos(q);
        var query = context.Aviones.AsNoTracking().Where(a => a.Activo);
        foreach (var t in terminos)
        {
            var termino = t;
            query = query.Where(a => a.Modelo.Contains(termino) || a.Matricula.Contains(termino) ||
                (a.CodigoIata != null && a.CodigoIata.Contains(termino)) || (a.CodigoIcao != null && a.CodigoIcao.Contains(termino)));
        }
        return await query.OrderBy(a => a.Modelo).ThenBy(a => a.Matricula)
            .Take(limite)
            .Select(a => new AvionResumenResponse(a.AvionId, a.Modelo, a.Matricula, a.CapacidadTotal))
            .ToListAsync(ct);
    }
}
