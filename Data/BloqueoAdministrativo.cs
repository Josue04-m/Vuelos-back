using Microsoft.EntityFrameworkCore;

namespace Vuelos_Backend.Data;

internal static class BloqueoAdministrativo
{
    // Un único orden de escritura administrativa evita carreras entre catálogos y programación de vuelos.
    // El bloqueo pertenece a la transacción; SQL Server lo libera al confirmar o revertir.
    public static Task AdquirirAsync(AppDbContext context, CancellationToken ct) => context.Database.ExecuteSqlRawAsync(
        "DECLARE @resultado int; EXEC @resultado = sys.sp_getapplock " +
        "@Resource = N'PacificNexus:CatalogosVuelos', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; " +
        "IF @resultado < 0 THROW 51001, 'No se pudo adquirir el bloqueo administrativo.', 1;", ct);
}
