namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Aeropuerto de la ruta consultada.</summary>
public sealed record AeropuertoVueloResponse(int AeropuertoId, string CodigoIata, string Nombre, string Ciudad, string Pais);

/// <summary>Datos del avión necesarios para consultar el vuelo.</summary>
public sealed record AvionVueloResponse(int AvionId, string Modelo, int CapacidadTotal);

/// <summary>Detalle público de un vuelo, con horarios y fechas en UTC.</summary>
public sealed record VueloResponse(
    int VueloId, string NumeroVuelo,
    AeropuertoVueloResponse Origen, AeropuertoVueloResponse Destino, AvionVueloResponse Avion,
    DateTimeOffset SalidaProgramada, DateTimeOffset LlegadaProgramada,
    DateTimeOffset? SalidaReal, DateTimeOffset? LlegadaReal,
    string Estado, bool PermiteReservas);

/// <summary>Página de vuelos. Una búsqueda sin resultados devuelve vuelos vacío.</summary>
public sealed record PaginaVuelosResponse(
    IReadOnlyList<VueloResponse> Vuelos, int Pagina, int TamanoPagina, int Total, int TotalPaginas);
