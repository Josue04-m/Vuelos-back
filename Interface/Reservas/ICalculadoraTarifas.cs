namespace Vuelos_Backend.Interface.Reservas;

public interface ICalculadoraTarifas
{
    void ValidarConfiguracion();
    decimal Calcular(decimal precioBase, string? tipoPasajero);
}
