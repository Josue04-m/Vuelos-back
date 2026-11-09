using System.Text.Json;
using Vuelos_Backend.Models.Pagos;

namespace Vuelos_Backend.Interface.Pagos;

public interface IPayPalClient
{
    Task<OrdenPayPal> CrearAsync(PagoPayPal pago, CancellationToken ct);
    Task<OrdenPayPal> ConsultarAsync(string ordenId, CancellationToken ct);
    Task<OrdenPayPal> CapturarAsync(PagoPayPal pago, CancellationToken ct);
    Task<bool> VerificarEventoAsync(JsonElement evento, IHeaderDictionary headers, CancellationToken ct);
}
