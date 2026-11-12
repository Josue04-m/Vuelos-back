using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Models.Common;

namespace Vuelos_Backend.Services.Catalogos;

public sealed class CatalogosAdminService(ICatalogosAdminRepository repository) : ICatalogosAdminService
{
    public Task<PaginaCatalogoResponse<AeropuertoAdminResponse>> ListarAeropuertosAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<Aeropuerto, AeropuertoAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<AvionAdminResponse>> ListarAvionesAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<Avion, AvionAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<CategoriaAdminResponse>> ListarCategoriasAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<CategoriaAsiento, CategoriaAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<AsientoFisicoAdminResponse>> ListarAsientosAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<AsientoFisico, AsientoFisicoAdminResponse>(f, Mapear, ct);
    public async Task<AeropuertoAdminResponse> ObtenerAeropuertoAsync(int id, CancellationToken ct) => Mapear(await Obtener<Aeropuerto>(id, ct));
    public async Task<AvionAdminResponse> ObtenerAvionAsync(int id, CancellationToken ct) => Mapear(await Obtener<Avion>(id, ct));
    public async Task<CategoriaAdminResponse> ObtenerCategoriaAsync(int id, CancellationToken ct) => Mapear(await Obtener<CategoriaAsiento>(id, ct));
    public async Task<AsientoFisicoAdminResponse> ObtenerAsientoAsync(int id, CancellationToken ct) => Mapear(await Obtener<AsientoFisico>(id, ct));

    private async Task<PaginaCatalogoResponse<R>> Listar<T, R>(FiltroCatalogoRequest filtro, Func<T, R> mapear, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        Validar(filtro);
        if (typeof(T) != typeof(AsientoFisico) && filtro.AvionId.HasValue)
            throw Invalido("AvionId solo aplica al catálogo de asientos físicos.");
        var (rows, total) = await repository.BuscarAsync<T>(filtro, ct);
        return new(rows.Select(mapear).ToArray(), filtro.Pagina, filtro.TamanoPagina, total,
            (int)((total + (long)filtro.TamanoPagina - 1) / filtro.TamanoPagina));
    }

    private async Task<T> Obtener<T>(int id, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        if (id <= 0) throw Invalido("El identificador debe ser positivo.");
        return await repository.ObtenerAsync<T>(id, ct) ?? throw new CatalogoOperacionException(ErrorCatalogo.NoEncontrado, "catalogo_no_encontrado", "Elemento del catálogo no encontrado.");
    }
    private static void Validar(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) throw Invalido(errors[0].ErrorMessage ?? "Solicitud inválida.");
    }
    private static DateTimeOffset? Utc(DateTime? fecha) => fecha.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc)) : null;
    private static AeropuertoAdminResponse Mapear(Aeropuerto a) => new(a.AeropuertoId, a.CodigoIata, a.Nombre, a.Ciudad, a.Pais, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion), MetadatosAeropuerto.Desde(a));
    private static AvionAdminResponse Mapear(Avion a) => new(a.AvionId, a.Modelo, a.Matricula, a.CapacidadTotal, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion), new MetadatosAvionResponse(a.CodigoIata, a.CodigoIcao, a.CantidadPrimeraClase, a.CantidadSegundaClase, a.CantidadTerceraClase, a.LetrasPorFila, a.DatosOrigenJson));
    private static CategoriaAdminResponse Mapear(CategoriaAsiento a) => new(a.CategoriaId, a.Nombre, a.Descripcion, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion));
    private static AsientoFisicoAdminResponse Mapear(AsientoFisico a) => new(a.AsientoFisicoId, a.AvionId, a.CategoriaId, a.CodigoAsiento, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion));
    private static CatalogoOperacionException Invalido(string message) => new(ErrorCatalogo.SolicitudInvalida, "solicitud_catalogo_invalida", message);
}
