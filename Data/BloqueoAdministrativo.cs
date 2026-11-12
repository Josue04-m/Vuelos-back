using Microsoft.EntityFrameworkCore;

namespace Vuelos_Backend.Data;

internal static class BloqueoAdministrativo
{
    // Un único orden de escritura administrativa evita carreras entre catálogos y programación de vuelos.
    // Clave fija PacificNexus:CatalogosVuelos (1347305793, 1), compartida por todos
    // los procesos. PostgreSQL libera este advisory lock al terminar la transacción.
    public static async Task AdquirirAsync(AppDbContext context, CancellationToken ct)
    {
        await ConsultasBloqueoPostgreSql.LimitarEsperaAsync(context, ct);
        await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1347305793, 1)", ct);
    }
}
