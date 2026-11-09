using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Ventas;
using Vuelos_Backend.Models.Ventas.DTOs;

namespace Vuelos_Backend.Repositories.Ventas;

public sealed class VentasRepository(AppDbContext context) : IVentasRepository
{
    internal IQueryable<AcumuladoVentas> ConsultaAcumulado(DateTime? desdeUtc, DateTime? hastaUtcExclusiva, string entorno = "Sandbox")
    {
        // Sin joins a reservas ni usuarios: cancelar un vuelo o eliminar un usuario no borra una compra registrada.
        var compras = context.PagosPayPal.AsNoTracking().Where(c => c.FechaCaptura != null && c.Entorno == entorno && c.Moneda == "USD");
        if (desdeUtc.HasValue) compras = compras.Where(c => c.FechaCaptura >= desdeUtc.Value);
        if (hastaUtcExclusiva.HasValue) compras = compras.Where(c => c.FechaCaptura < hastaUtcExclusiva.Value);
        // Una sola consulta devuelve suma y conteo del mismo conjunto.
        return compras.GroupBy(c => 1).Select(g => new AcumuladoVentas(g.Sum(c => c.Monto), g.LongCount()));
    }

    public async Task<AcumuladoVentas> AcumularAsync(DateTime? desdeUtc, DateTime? hastaUtcExclusiva, CancellationToken ct, string entorno = "Sandbox") =>
        await ConsultaAcumulado(desdeUtc, hastaUtcExclusiva, entorno).SingleOrDefaultAsync(ct) ?? new(0m, 0);
}
