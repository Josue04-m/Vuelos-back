namespace Vuelos_Backend.Models.Reservas;

public sealed class OpcionesTarifasPasajero
{
    public const string Seccion = "TarifasPasajero";
    public Dictionary<string, decimal> Porcentajes { get; set; } = [];
}