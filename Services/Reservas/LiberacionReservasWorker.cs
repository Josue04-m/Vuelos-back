using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Reservas;

namespace Vuelos_Backend.Services.Reservas;

public sealed class LiberacionReservasWorker(IServiceScopeFactory scopes, IOptionsMonitor<OpcionesReservas> options,
    TimeProvider clock, ILogger<LiberacionReservasWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IExpiracionReservasService>().ExpirarLoteAsync(null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("No se completó la liberación de reservas. Tipo: {Tipo}", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(options.CurrentValue.IntervaloLiberacionSegundos), clock, stoppingToken);
        }
    }
}
