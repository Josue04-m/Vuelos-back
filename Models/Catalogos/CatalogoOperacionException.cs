namespace Vuelos_Backend.Models.Catalogos;

public enum TipoCatalogo { Aeropuertos, Aviones, Categorias, AsientosFisicos }
public enum ErrorCatalogo { SolicitudInvalida, NoEncontrado, Conflicto }
public sealed class CatalogoOperacionException(ErrorCatalogo error, string codigo, string mensaje) : Exception(mensaje)
{
    public ErrorCatalogo Error { get; } = error;
    public string Codigo { get; } = codigo;
}
