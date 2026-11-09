using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Data;
using Vuelos_Backend.Models.Pagos;

namespace Vuelos_Backend.Services.Pagos;

/// <summary>Solo consulta órdenes: nunca inicia capturas ni nuevos cobros.</summary>
public sealed class ConciliacionPayPalWorker(IServiceScopeFactory scopes, IOptions<OpcionesPayPal> opciones,
    TimeProvider clock, ILogger<ConciliacionPayPalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (opciones.Value.Habilitado)
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var limite = clock.GetUtcNow().UtcDateTime.AddSeconds(-60);
                    var ids = await db.PagosPayPal.AsNoTracking().Where(p => p.BloqueaReserva && p.OrdenId != null &&
                        p.Entorno == opciones.Value.Entorno && (p.OperacionHasta == null || p.OperacionHasta <= limite))
                        .OrderBy(p => p.FechaActualizacion).Select(p => p.PagoId).Take(50).ToListAsync(stoppingToken);
                    foreach (var id in ids)
                    {
                        try { await scope.ServiceProvider.GetRequiredService<PagoPayPalService>().ConciliarAsync(id, stoppingToken); }
                        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                        { logger.LogWarning("Conciliación PayPal pendiente para {PagoId}. Tipo: {Tipo}", id, e.GetType().Name); }
                    }
                }
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            { logger.LogWarning("Conciliación PayPal no disponible. Tipo: {Tipo}", e.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(60), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
