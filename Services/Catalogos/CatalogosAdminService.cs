using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;
using Vuelos_Backend.Models.Common;

namespace Vuelos_Backend.Services.Catalogos;

public sealed class CatalogosAdminService(ICatalogosAdminRepository repository, TimeProvider clock) : ICatalogosAdminService
{
    public Task<PaginaCatalogoResponse<AeropuertoAdminResponse>> ListarAeropuertosAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<Aeropuerto, AeropuertoAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<AvionAdminResponse>> ListarAvionesAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<Avion, AvionAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<CategoriaAdminResponse>> ListarCategoriasAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<CategoriaAsiento, CategoriaAdminResponse>(f, Mapear, ct);
    public Task<PaginaCatalogoResponse<AsientoFisicoAdminResponse>> ListarAsientosAsync(FiltroCatalogoRequest f, CancellationToken ct) => Listar<AsientoFisico, AsientoFisicoAdminResponse>(f, Mapear, ct);
    public async Task<AeropuertoAdminResponse> ObtenerAeropuertoAsync(int id, CancellationToken ct) => Mapear(await Obtener<Aeropuerto>(id, ct));
    public async Task<AvionAdminResponse> ObtenerAvionAsync(int id, CancellationToken ct) => Mapear(await Obtener<Avion>(id, ct));
    public async Task<CategoriaAdminResponse> ObtenerCategoriaAsync(int id, CancellationToken ct) => Mapear(await Obtener<CategoriaAsiento>(id, ct));
    public async Task<AsientoFisicoAdminResponse> ObtenerAsientoAsync(int id, CancellationToken ct) => Mapear(await Obtener<AsientoFisico>(id, ct));

    public Task<AeropuertoAdminResponse> GuardarAeropuertoAsync(int? id, GuardarAeropuertoRequest r, CancellationToken ct)
    {
        Validar(r);
        return Guardar<Aeropuerto, AeropuertoAdminResponse>(id, r.RowVersion, async a =>
        {
            var codigo = r.CodigoIata.Trim().ToUpperInvariant();
            var nombre = r.Nombre.Trim(); var ciudad = r.Ciudad.Trim(); var pais = r.Pais.Trim();
            if (id.HasValue && (a.CodigoIata != codigo || a.Nombre != nombre || a.Ciudad != ciudad || a.Pais != pais))
                await ExigirSinVuelos(TipoCatalogo.Aeropuertos, id.Value, false, ct);
            a.CodigoIata = codigo; a.Nombre = nombre; a.Ciudad = ciudad; a.Pais = pais;
        }, Mapear, ct);
    }

    public Task<AvionAdminResponse> GuardarAvionAsync(int? id, GuardarAvionRequest r, CancellationToken ct)
    {
        Validar(r);
        if (!id.HasValue) throw Invalido("Use la creación de avión con generación de asientos.");
        return Guardar<Avion, AvionAdminResponse>(id, r.RowVersion, async a =>
        {
            var modelo = r.Modelo.Trim(); var matricula = r.Matricula.Trim().ToUpperInvariant();
            if (id.HasValue)
            {
                if (a.Modelo != modelo || a.Matricula != matricula || a.CapacidadTotal != r.CapacidadTotal)
                    await ExigirSinVuelos(TipoCatalogo.Aviones, id.Value, false, ct);
                if (await repository.ContarAsientosActivosAsync(id.Value, null, ct) > r.CapacidadTotal)
                    throw Conflicto("capacidad_insuficiente", "La capacidad no puede ser menor que la cantidad de asientos físicos activos.");
            }
            a.Modelo = modelo; a.Matricula = matricula; a.CapacidadTotal = r.CapacidadTotal;
        }, Mapear, ct);
    }

    public async Task<AvionAdminResponse> CrearAvionAsync(CrearAvionRequest request, CancellationToken ct)
    {
        Validar(request);
        await using var transaction = await repository.IniciarTransaccionAsync(ct);
        var response = Mapear(await CreacionAviones.CrearAsync(repository, request, ct));
        await transaction.ConfirmarAsync(ct);
        return response;
    }
    public Task<CategoriaAdminResponse> GuardarCategoriaAsync(int? id, GuardarCategoriaRequest r, CancellationToken ct)
    {
        Validar(r);
        return Guardar<CategoriaAsiento, CategoriaAdminResponse>(id, r.RowVersion, async a =>
        {
            var nombre = r.Nombre.Trim(); var descripcion = r.Descripcion?.Trim();
            if (id.HasValue && (a.Nombre != nombre || a.Descripcion != descripcion))
                await ExigirSinVuelos(TipoCatalogo.Categorias, id.Value, false, ct);
            a.Nombre = nombre; a.Descripcion = descripcion;
        }, Mapear, ct);
    }

    public Task<AsientoFisicoAdminResponse> GuardarAsientoAsync(int? id, GuardarAsientoFisicoRequest r, CancellationToken ct)
    {
        Validar(r);
        return Guardar<AsientoFisico, AsientoFisicoAdminResponse>(id, r.RowVersion, async a =>
        {
            var codigo = r.CodigoAsiento.Trim().ToUpperInvariant();
            var cambia = !id.HasValue || a.AvionId != r.AvionId || a.CategoriaId != r.CategoriaId || a.CodigoAsiento != codigo;
            if (id.HasValue && cambia && await repository.AsientoFueAsignadoAsync(id.Value, ct))
                throw Conflicto("asiento_con_historial", "No puede modificar un asiento físico que ya fue asignado a un vuelo.");
            var avion = await Obtener<Avion>(r.AvionId, ct);
            var categoria = await Obtener<CategoriaAsiento>(r.CategoriaId, ct);
            if (!avion.Activo || !categoria.Activo) throw Invalido("El avión y la categoría deben estar activos.");
            if (cambia)
            {
                await ExigirSinVuelos(TipoCatalogo.Aviones, r.AvionId, true, ct);
                if (id.HasValue && a.AvionId != r.AvionId) await ExigirSinVuelos(TipoCatalogo.Aviones, a.AvionId, true, ct);
            }
            if (await repository.ContarAsientosActivosAsync(r.AvionId, id, ct) >= avion.CapacidadTotal)
                throw Conflicto("capacidad_excedida", "El asiento supera la capacidad del avión.");
            a.AvionId = r.AvionId; a.CategoriaId = r.CategoriaId; a.CodigoAsiento = codigo;
        }, Mapear, ct);
    }

    public Task DesactivarAsync(TipoCatalogo tipo, int id, DesactivarCatalogoRequest r, CancellationToken ct)
    {
        Validar(r);
        return tipo switch
        {
            TipoCatalogo.Aeropuertos => Desactivar<Aeropuerto>(tipo, id, r, ct),
            TipoCatalogo.Aviones => Desactivar<Avion>(tipo, id, r, ct),
            TipoCatalogo.Categorias => Desactivar<CategoriaAsiento>(tipo, id, r, ct),
            TipoCatalogo.AsientosFisicos => Desactivar<AsientoFisico>(tipo, id, r, ct),
            _ => throw Invalido("Catálogo no soportado.")
        };
    }

    private async Task Desactivar<T>(TipoCatalogo tipo, int id, DesactivarCatalogoRequest request, CancellationToken ct) where T : class, ICatalogoAdministrable
    {
        await using var transaction = await repository.IniciarTransaccionAsync(ct);
        var entity = await Obtener<T>(id, ct);
        ValidarVersion(entity, request.RowVersion);
        if (entity.Activo)
        {
            await ExigirSinVuelos(tipo, id, true, ct);
            if (entity is CategoriaAsiento && await repository.CategoriaTieneAsientosActivosAsync(id, ct))
                throw Conflicto("categoria_en_uso", "Debe desactivar o reasignar sus asientos activos antes de desactivar la categoría.");
            if (entity is AsientoFisico asiento)
                await ExigirSinVuelos(TipoCatalogo.Aviones, asiento.AvionId, true, ct);
            await repository.DesactivarAsync<T>(id, entity.RowVersion, clock.GetUtcNow().UtcDateTime, ct);
        }
        await transaction.ConfirmarAsync(ct);
    }

    private async Task<TResponse> Guardar<T, TResponse>(int? id, string? version, Func<T, Task> aplicar,
        Func<T, TResponse> mapear, CancellationToken ct) where T : class, ICatalogoAdministrable, new()
    {
        if (!id.HasValue && version is not null) throw Invalido("No envíe RowVersion al crear.");
        await using var transaction = await repository.IniciarTransaccionAsync(ct);
        var entity = id.HasValue ? await Obtener<T>(id.Value, ct) : new T();
        if (id.HasValue)
        {
            ValidarVersion(entity, version);
            if (!entity.Activo) throw Conflicto("elemento_inactivo", "El elemento está desactivado y no admite edición.");
        }
        await aplicar(entity);
        if (id.HasValue) await repository.ActualizarAsync(entity, ct);
        else await repository.CrearAsync(entity, ct);
        var resultado = mapear(await Obtener<T>(Id(entity), ct));
        await transaction.ConfirmarAsync(ct);
        return resultado;
    }

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
    private async Task ExigirSinVuelos(TipoCatalogo tipo, int id, bool soloVigentes, CancellationToken ct)
    {
        if (await repository.TieneVuelosAsync(tipo, id, soloVigentes, ct))
            throw Conflicto("catalogo_en_uso", soloVigentes ? "El elemento participa en vuelos operativos. Finalice o cancele esos vuelos antes de modificarlo."
                : "El elemento ya está asociado a vuelos. No puede alterar sus datos históricos.");
    }
    private static void ValidarVersion(ICatalogoAdministrable entity, string? version)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (version is null) throw Conflicto("version_requerida", "Consulte el elemento y envíe su RowVersion vigente.");
        if (!Convert.TryFromBase64String(version, bytes, out var count) || count != 8) throw Invalido("RowVersion debe ser Base64 de 8 bytes.");
        if (!bytes.SequenceEqual(entity.RowVersion)) throw Conflicto("version_desactualizada", "La información cambió. Consulte su RowVersion vigente.");
    }
    private static void Validar(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) throw Invalido(errors[0].ErrorMessage ?? "Solicitud inválida.");
    }
    private static int Id<T>(T entity) => entity switch { Aeropuerto a => a.AeropuertoId, Avion a => a.AvionId, CategoriaAsiento a => a.CategoriaId, AsientoFisico a => a.AsientoFisicoId, _ => throw Invalido("Catálogo no soportado.") };
    private static DateTimeOffset? Utc(DateTime? fecha) => fecha.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc)) : null;
    private static AeropuertoAdminResponse Mapear(Aeropuerto a) => new(a.AeropuertoId, a.CodigoIata, a.Nombre, a.Ciudad, a.Pais, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion), MetadatosAeropuerto.Desde(a));
    private static AvionAdminResponse Mapear(Avion a) => new(a.AvionId, a.Modelo, a.Matricula, a.CapacidadTotal, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion), new MetadatosAvionResponse(a.CodigoIata, a.CodigoIcao, a.CantidadPrimeraClase, a.CantidadSegundaClase, a.CantidadTerceraClase, a.LetrasPorFila, a.DatosOrigenJson));
    private static CategoriaAdminResponse Mapear(CategoriaAsiento a) => new(a.CategoriaId, a.Nombre, a.Descripcion, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion));
    private static AsientoFisicoAdminResponse Mapear(AsientoFisico a) => new(a.AsientoFisicoId, a.AvionId, a.CategoriaId, a.CodigoAsiento, a.Activo, Utc(a.FechaDesactivacion), Convert.ToBase64String(a.RowVersion));
    private static CatalogoOperacionException Invalido(string message) => new(ErrorCatalogo.SolicitudInvalida, "solicitud_catalogo_invalida", message);
    private static CatalogoOperacionException Conflicto(string code, string message) => new(ErrorCatalogo.Conflicto, code, message);
}
