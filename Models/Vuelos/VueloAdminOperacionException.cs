namespace Vuelos_Backend.Models.Vuelos;

public enum ErrorVueloAdmin { SolicitudInvalida, NoEncontrado, Conflicto }

public sealed class VueloAdminOperacionException(
    ErrorVueloAdmin error, string codigo, string mensaje) : Exception(mensaje)
{
    public ErrorVueloAdmin Error { get; } = error;
    public string Codigo { get; } = codigo;
}
