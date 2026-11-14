namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Aeropuerto de un recorrido. País ISO para bandera; coordenadas WGS84 en grados o null si no son utilizables.</summary>
public sealed record AeropuertoMapaAdminResponse(int AeropuertoId, string CodigoIata, string Nombre,
    string? Ciudad, string Pais, double? Latitud, double? Longitud, bool CoordenadasDisponibles);

/// <summary>Extremos de un recorrido ilustrativo. No contiene trayectoria operacional ni telemetría.</summary>
public sealed record RecorridoVueloAdminResponse(int VueloId, string NumeroVuelo, string Estado,
    AeropuertoMapaAdminResponse Origen, AeropuertoMapaAdminResponse Destino,
    bool PuedeMostrarRecorrido, string? MotivoNoDisponible, bool? CruzaAntimeridiano,
    string TipoRecorrido, bool EsSeguimientoEnTiempoReal);
