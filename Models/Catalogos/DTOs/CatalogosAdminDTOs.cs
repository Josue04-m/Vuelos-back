using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Filtros administrativos. Activo=true por defecto; IncluirInactivos=true incluye ambos estados. AvionId solo para asientos físicos.</summary>
public sealed record FiltroCatalogoRequest : IValidatableObject
{
    [StringLength(100)] public string? Buscar { get; init; }
    public bool? Activo { get; init; } = true;
    public bool IncluirInactivos { get; init; }
    [Range(1, int.MaxValue)] public int? AvionId { get; init; }
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 20;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((long)(Pagina - 1) * TamanoPagina > int.MaxValue)
            yield return new ValidationResult("La página supera el desplazamiento permitido.", [nameof(Pagina)]);
    }
}

/// <summary>Página de un catálogo administrativo.</summary>
public sealed record PaginaCatalogoResponse<T>(IReadOnlyList<T> Elementos, int Pagina, int TamanoPagina, int Total, int TotalPaginas);
/// <summary>Aeropuerto administrativo, con estado y versión.</summary>
public sealed record AeropuertoAdminResponse(int AeropuertoId, string CodigoIata, string Nombre, string? Ciudad, string Pais,
    bool Activo, DateTimeOffset? FechaDesactivacion, string RowVersion, MetadatosAeropuertoResponse? Metadatos = null);
/// <summary>Avión administrativo, con estado y versión.</summary>
public sealed record AvionAdminResponse(int AvionId, string Modelo, string Matricula, int CapacidadTotal,
    bool Activo, DateTimeOffset? FechaDesactivacion, string RowVersion, MetadatosAvionResponse? Metadatos = null);
/// <summary>Categoría administrativa, con estado y versión.</summary>
public sealed record CategoriaAdminResponse(int CategoriaId, string Nombre, string? Descripcion,
    bool Activo, DateTimeOffset? FechaDesactivacion, string RowVersion);
/// <summary>Asiento físico administrativo, con estado y versión.</summary>
public sealed record AsientoFisicoAdminResponse(int AsientoFisicoId, int AvionId, int CategoriaId, string CodigoAsiento,
    bool Activo, DateTimeOffset? FechaDesactivacion, string RowVersion);

