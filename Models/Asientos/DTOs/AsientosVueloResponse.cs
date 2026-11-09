namespace Vuelos_Backend.Models.Asientos.DTOs;

public sealed record CategoriaAsientoResponse(int CategoriaId, string Nombre, string? Descripcion);

public sealed record AsientoVueloResponse(
    int AsientoVueloId, string CodigoAsiento, CategoriaAsientoResponse Categoria,
    decimal Precio, string Moneda, string Estado, bool Disponible, string RowVersion);

public sealed record AsientosVueloResponse(
    int VueloId, string NumeroVuelo, string Moneda, bool PermiteReservas,
    IReadOnlyList<AsientoVueloResponse> Asientos);
