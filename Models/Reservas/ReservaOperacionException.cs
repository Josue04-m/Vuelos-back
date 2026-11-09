namespace Vuelos_Backend.Models.Reservas;

public enum ErrorReserva { SolicitudInvalida, NoEncontrada, Conflicto, TarifasSinConfigurar, ServicioNoDisponible }

public sealed class ReservaOperacionException(
    ErrorReserva error, string codigo, string mensaje, decimal? montoTotalActual = null) : Exception(mensaje)
{
    public ErrorReserva Error { get; } = error;
    public string Codigo { get; } = codigo;
    public decimal? MontoTotalActual { get; } = montoTotalActual;
}
