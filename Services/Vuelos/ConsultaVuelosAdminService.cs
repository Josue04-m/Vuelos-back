using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Vuelos;
using Vuelos_Backend.Models.Vuelos.DTOs;

namespace Vuelos_Backend.Services.Vuelos;

/// <summary>Consultas administrativas de solo lectura para listado y mapa.</summary>
public sealed class ConsultaVuelosAdminService(IVueloRepository vuelos, TimeProvider clock) : IConsultaVuelosAdminService
{
    public async Task<PaginaVuelosResponse> BuscarAsync(BusquedaVuelosAdminRequest request, CancellationToken ct)
    {
        var desde = request.Fecha ?? request.FechaDesde;
        var hasta = request.Fecha ?? request.FechaHasta;
        var filtro = new FiltroVuelos(request.Origen?.ToUpperInvariant(), request.Destino?.ToUpperInvariant(),
            desde?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            hasta is { } fin && fin != DateOnly.MaxValue ? fin.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null,
            request.Pagina, request.TamanoPagina, request.PaisOrigen?.ToUpperInvariant(), request.PaisDestino?.ToUpperInvariant(),
            request.OrdenarPor, request.Direccion == "desc");
        var resultado = await vuelos.BuscarAsync(filtro, ct);
        var ahoraUtc = clock.GetUtcNow().UtcDateTime;
        return new(resultado.Vuelos.Select(v => VueloService.Mapear(v, ahoraUtc)).ToArray(),
            request.Pagina, request.TamanoPagina, resultado.Total,
            (int)((resultado.Total + (long)request.TamanoPagina - 1) / request.TamanoPagina));
    }

    public async Task<RecorridoVueloAdminResponse?> ObtenerRecorridoAsync(int vueloId, CancellationToken ct)
    {
        var vuelo = await vuelos.ObtenerAsync(vueloId, ct);
        if (vuelo is null) return null;
        var origen = Punto(vuelo.AeropuertoOrigen);
        var destino = Punto(vuelo.AeropuertoDestino);
        var disponible = origen.CoordenadasDisponibles && destino.CoordenadasDisponibles;
        var motivo = disponible ? null : !origen.CoordenadasDisponibles && !destino.CoordenadasDisponibles
            ? "coordenadas_origen_y_destino_no_disponibles"
            : !origen.CoordenadasDisponibles ? "coordenadas_origen_no_disponibles" : "coordenadas_destino_no_disponibles";
        return new(vuelo.VueloId, vuelo.NumeroVuelo, vuelo.Estado, origen, destino, disponible, motivo,
            disponible ? Math.Abs(origen.Longitud!.Value - destino.Longitud!.Value) > 180 : null,
            "orientativo_entre_aeropuertos", false);
    }

    private static AeropuertoMapaAdminResponse Punto(Aeropuerto aeropuerto)
    {
        var lat = aeropuerto.Latitud;
        var lon = aeropuerto.Longitud;
        var valida = lat.HasValue && lon.HasValue && double.IsFinite(lat.Value) && double.IsFinite(lon.Value)
            && lat.Value is >= -90 and <= 90 && lon.Value is >= -180 and <= 180;
        // La pareja es indivisible; nunca sustituir coordenadas ausentes por (0,0).
        return new(aeropuerto.AeropuertoId, aeropuerto.CodigoIata, aeropuerto.Nombre, aeropuerto.Ciudad,
            aeropuerto.Pais, valida ? lat : null, valida ? lon : null, valida);
    }
}
