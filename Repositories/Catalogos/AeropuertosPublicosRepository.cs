using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Services.Catalogos;

namespace Vuelos_Backend.Repositories.Catalogos;

public sealed class AeropuertosPublicosRepository(AppDbContext context) : IAeropuertosPublicosRepository
{
    internal IQueryable<Aeropuerto> Consulta(string buscar)
    {
        var paises = PaisesBusqueda.Resolver(buscar);
        return context.Aeropuertos.AsNoTracking().Where(a => a.Activo &&
            Regex.IsMatch(a.CodigoIata, "^[A-Z]{3}$") && paises.Contains(a.Pais));
    }

    public async Task<PaginaAeropuertosPublicosResponse> BuscarAsync(BuscarAeropuertosPublicosRequest request, CancellationToken ct)
    {
        var query = Consulta(request.Buscar);
        var total = await query.CountAsync(ct);
        var salto = ((long)request.Pagina - 1) * request.TamanoPagina;
        // Evita overflow y consultas inútiles para páginas posteriores a la última.
        var elementos = salto >= total ? new List<AeropuertoPublicoResponse>() : await Pagina(query, (int)salto, request.TamanoPagina).ToListAsync(ct);
        return new(elementos, request.Pagina, request.TamanoPagina, total, (int)Math.Ceiling(total / (double)request.TamanoPagina));
    }

    internal static IQueryable<AeropuertoPublicoResponse> Pagina(IQueryable<Aeropuerto> query, int salto, int tamano) => query
            .OrderBy(a => a.Ciudad).ThenBy(a => a.Nombre).ThenBy(a => a.AeropuertoId)
            .Skip(salto).Take(tamano)
            .Select(a => new AeropuertoPublicoResponse(a.AeropuertoId, a.CodigoIata, a.Nombre, a.Ciudad, a.Pais));
}
