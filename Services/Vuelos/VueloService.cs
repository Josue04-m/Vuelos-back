using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos.DTOs;
using Vuelos_Backend.Models.Vuelos;
using Vuelos_Backend.Models.Vuelos.DTOs;
namespace Vuelos_Backend.Services.Vuelos;

public sealed class VueloService(IVueloRepository vuelos, TimeProvider clock, IExpiracionReservasService expiracion) : IVueloService
{
    public async Task<PaginaVuelosResponse> BuscarAsync(
        BusquedaVuelosRequest request, CancellationToken cancellationToken)
    {
        var desde = request.Fecha ?? request.FechaDesde;
        var hasta = request.Fecha ?? request.FechaHasta;
        var filtro = new FiltroVuelos(
            request.Origen?.ToUpperInvariant(), request.Destino?.ToUpperInvariant(),
            desde?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            hasta is { } fin && fin != DateOnly.MaxValue
                ? fin.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null,
            request.Pagina, request.TamanoPagina);
        var resultado = await vuelos.BuscarAsync(filtro, cancellationToken);
        var ahoraUtc = clock.GetUtcNow().UtcDateTime;
        return new PaginaVuelosResponse(
            resultado.Vuelos.Select(v => Mapear(v, ahoraUtc)).ToArray(),
            request.Pagina, request.TamanoPagina, resultado.Total,
            (int)((resultado.Total + (long)request.TamanoPagina - 1) / request.TamanoPagina));
    }

    public async Task<VueloResponse?> ObtenerAsync(int vueloId, CancellationToken cancellationToken)
    {
        var vuelo = await vuelos.ObtenerAsync(vueloId, cancellationToken);
        return vuelo is null ? null : Mapear(vuelo, clock.GetUtcNow().UtcDateTime);
    }

    public async Task<AsientosVueloResponse?> ListarAsientosAsync(int vueloId, CancellationToken cancellationToken)
    {
        await expiracion.ExpirarVueloAsync(vueloId, cancellationToken);
        var vuelo = await vuelos.ObtenerAsync(vueloId, cancellationToken);
        if (vuelo is null) return null;
        var asientos = await vuelos.ListarAsientosAsync(vueloId, cancellationToken);
        var permiteReservas = PermiteReservas(vuelo, clock.GetUtcNow().UtcDateTime);
        return new AsientosVueloResponse(vuelo.VueloId, vuelo.NumeroVuelo, "USD", permiteReservas,
            asientos.Select(a => new AsientoVueloResponse(
                a.AsientoVueloId, a.AsientoFisico.CodigoAsiento,
                new CategoriaAsientoResponse(a.AsientoFisico.Categoria.CategoriaId,
                    a.AsientoFisico.Categoria.Nombre, a.AsientoFisico.Categoria.Descripcion),
                a.Precio, "USD", a.Estado,
                permiteReservas && a.Estado == "disponible",
                Convert.ToBase64String(a.RowVersion))).ToArray());
    }

    private static bool PermiteReservas(Vuelo vuelo, DateTime ahoraUtc) =>
        vuelo.Estado is "programado" or "retrasado" && !vuelo.SalidaReal.HasValue &&
        !vuelo.LlegadaReal.HasValue && vuelo.SalidaProgramada > ahoraUtc;

    // datetime2 no conserva Kind. El conjunto de datos de este proyecto debe contener UTC.
    private static DateTimeOffset Utc(DateTime fecha) =>
        new(DateTime.SpecifyKind(fecha, DateTimeKind.Utc));

    private static AeropuertoVueloResponse Mapear(Aeropuerto a) =>
        new(a.AeropuertoId, a.CodigoIata, a.Nombre, a.Ciudad, a.Pais);

    internal static VueloResponse Mapear(Vuelo v, DateTime ahoraUtc) =>
        new(v.VueloId, v.NumeroVuelo, Mapear(v.AeropuertoOrigen), Mapear(v.AeropuertoDestino),
            new AvionVueloResponse(v.Avion.AvionId, v.Avion.Modelo, v.Avion.CapacidadTotal),
            Utc(v.SalidaProgramada), Utc(v.LlegadaProgramada),
            v.SalidaReal is { } salida ? Utc(salida) : null,
            v.LlegadaReal is { } llegada ? Utc(llegada) : null,
            v.Estado, PermiteReservas(v, ahoraUtc));
}
