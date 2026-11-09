using System.ComponentModel.DataAnnotations;
using Vuelos_Backend.Models.Reservas.DTOs;

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

/// <summary>Versión vigente necesaria para desactivar un elemento.</summary>
public sealed record DesactivarCatalogoRequest
{
    [Required, VersionFila] public required string RowVersion { get; init; }
}

/// <summary>Crear o editar un aeropuerto. RowVersion se exige en edición y se omite al crear.</summary>
public sealed record GuardarAeropuertoRequest
{
    [Required, RegularExpression("^[A-Za-z]{3}$")] public required string CodigoIata { get; init; }
    [Required, StringLength(150)] public required string Nombre { get; init; }
    [Required, StringLength(100)] public required string Ciudad { get; init; }
    [Required, StringLength(100)] public required string Pais { get; init; }
    [VersionCatalogoOpcional] public string? RowVersion { get; init; }
}

/// <summary>Crea el avión y genera todos sus asientos físicos en una transacción.</summary>
public sealed record CrearAvionRequest
{
    [Required, StringLength(100)] public required string Modelo { get; init; }
    [Required, StringLength(50)] public required string Matricula { get; init; }
    /// <summary>Número exacto de asientos a generar; máximo técnico de 1000 por operación.</summary>
    [Range(1, 1000)] public required int CapacidadTotal { get; init; }
    /// <summary>Compatibilidad: categoría única. Use DistribucionClases para distribuir varias clases; no combine ambas opciones.</summary>
    [Range(1, int.MaxValue)] public int? CategoriaId { get; init; }
    /// <summary>Categorías y cantidades, en orden de generación; su suma debe ser igual a CapacidadTotal.</summary>
    [MinLength(1), MaxLength(1000)]
    public IReadOnlyList<DistribucionClaseRequest>? DistribucionClases { get; init; }
    /// <summary>ABC produce 1A, 1B, 1C, 2A... Vacío o null produce 1, 2, 3... No repetir letras.</summary>
    [StringLength(26), RegularExpression("^[A-Za-z]*$")]
    public string? LetrasPorFila { get; init; } = "ABC";
}

/// <summary>Cantidad de asientos de una clase del catálogo (Primera, Segunda o Tercera Clase).</summary>
public sealed record DistribucionClaseRequest
{
    [Range(1, int.MaxValue)] public required int CategoriaId { get; init; }
    [Range(1, 1000)] public required int Cantidad { get; init; }
}

/// <summary>Edita los datos del avión; no regenera ni elimina asientos.</summary>
public sealed record GuardarAvionRequest
{
    [Required, StringLength(100)] public required string Modelo { get; init; }
    [Required, StringLength(50)] public required string Matricula { get; init; }
    [Range(1, int.MaxValue)] public required int CapacidadTotal { get; init; }
    [VersionCatalogoOpcional] public string? RowVersion { get; init; }
}

/// <summary>Crear o editar una categoría de asiento.</summary>
public sealed record GuardarCategoriaRequest
{
    [Required, StringLength(50)] public required string Nombre { get; init; }
    [StringLength(255)] public string? Descripcion { get; init; }
    [VersionCatalogoOpcional] public string? RowVersion { get; init; }
}

/// <summary>Crear o editar un asiento físico del avión, sin precio ni disponibilidad por vuelo.</summary>
public sealed record GuardarAsientoFisicoRequest
{
    [Range(1, int.MaxValue)] public required int AvionId { get; init; }
    [Range(1, int.MaxValue)] public required int CategoriaId { get; init; }
    [Required, StringLength(10), RegularExpression("^[A-Za-z0-9-]+$")] public required string CodigoAsiento { get; init; }
    [VersionCatalogoOpcional] public string? RowVersion { get; init; }
}

/// <summary>Página de un catálogo administrativo.</summary>
public sealed record PaginaCatalogoResponse<T>(IReadOnlyList<T> Elementos, int Pagina, int TamanoPagina, int Total, int TotalPaginas);
/// <summary>Aeropuerto administrativo, con estado y versión.</summary>
public sealed record AeropuertoAdminResponse(int AeropuertoId, string CodigoIata, string Nombre, string Ciudad, string Pais,
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

public sealed class VersionCatalogoOpcionalAttribute : ValidationAttribute
{
    public VersionCatalogoOpcionalAttribute() => ErrorMessage = "RowVersion debe ser Base64 de 8 bytes.";
    public override bool IsValid(object? value) => value is null || new VersionFilaAttribute().IsValid(value);
}
