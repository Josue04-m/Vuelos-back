using Vuelos_Backend.Interface.Reservas;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Models.Reservas;
namespace Vuelos_Backend.Services.Reservas;

public sealed class CalculadoraTarifas(IOptions<OpcionesTarifasPasajero> opciones) : ICalculadoraTarifas
{
    private IReadOnlyDictionary<string, decimal>? porcentajes;

    public void ValidarConfiguracion()
    {
        var valores = opciones.Value.Porcentajes;
        if (new[] { "adulto", "joven", "nino", "bebe" }.Any(tipo =>
                !valores.TryGetValue(tipo, out var porcentaje) || porcentaje < 0))
            throw SinConfiguracion();
        porcentajes = new Dictionary<string, decimal>(valores);
    }

    public decimal Calcular(decimal precioBase, string? tipoPasajero)
    {
        if (porcentajes is null) throw SinConfiguracion();
        if (tipoPasajero is null || !porcentajes.TryGetValue(tipoPasajero, out var porcentaje))
            throw new ReservaOperacionException(ErrorReserva.Conflicto, "tipo_pasajero_pendiente",
                "El detalle no tiene un tipo de pasajero válido. Complete sus datos antes de cambiar el asiento.");
        try
        {
            var importe = decimal.Round(checked(precioBase * (porcentaje / 100m)), 2, MidpointRounding.AwayFromZero);
            if (importe is < 0 or > 99999999.99m) throw new OverflowException();
            return importe;
        }
        catch (OverflowException)
        {
            throw new ReservaOperacionException(ErrorReserva.TarifasSinConfigurar, "tarifa_fuera_de_rango",
                "La tarifa configurada supera el importe admitido por el esquema.");
        }
    }

    private static ReservaOperacionException SinConfiguracion() =>
        new(ErrorReserva.TarifasSinConfigurar, "tarifas_sin_configurar",
            "Configure TarifasPasajero:Porcentajes para adulto, joven, nino y bebe.");
}
