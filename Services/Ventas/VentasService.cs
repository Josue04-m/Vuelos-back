using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Interface.Ventas;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Ventas.DTOs;

namespace Vuelos_Backend.Services.Ventas;

public sealed class VentasService(IVentasRepository repository) : IVentasService
{
    public async Task<ResumenVentasResponse> ObtenerResumenAsync(FiltroVentasRequest filtro, CancellationToken ct)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(filtro, new ValidationContext(filtro), errores, true))
            throw new ReservaOperacionException(ErrorReserva.SolicitudInvalida, "rango_ventas_invalido", "Hasta debe ser igual o posterior a Desde.");
        var desde = filtro.Desde?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hasta = filtro.Hasta is { } fin && fin != DateOnly.MaxValue
            ? fin.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : (DateTime?)null;
        var total = await repository.AcumularAsync(desde, hasta, ct, filtro.Entorno);
        return new(total.Total, total.Cantidad, "USD", "PayPal", filtro.Entorno, filtro.Entorno == "Sandbox", filtro.Desde, filtro.Hasta);
    }
}
