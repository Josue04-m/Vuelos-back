using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Services.Catalogos;

// Datos internos usados exclusivamente para generar aviones y asientos durante la carga CSV.
internal sealed record DatosCreacionAvion
{
    [Required, StringLength(100)] public required string Modelo { get; init; }
    [Required, StringLength(50)] public required string Matricula { get; init; }
    [Range(1, 1000)] public required int CapacidadTotal { get; init; }
    [MinLength(1), MaxLength(1000)] public IReadOnlyList<DistribucionClase>? DistribucionClases { get; init; }
    [StringLength(26), RegularExpression("^[A-Za-z]*$")] public string? LetrasPorFila { get; init; } = "ABC";
}

internal sealed record DistribucionClase
{
    [Range(1, int.MaxValue)] public required int CategoriaId { get; init; }
    [Range(1, 1000)] public required int Cantidad { get; init; }
}
