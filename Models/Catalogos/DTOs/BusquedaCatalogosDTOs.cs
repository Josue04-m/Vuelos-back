using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Búsqueda ligera para selectores. Q admite varias palabras; todas deben coincidir. Sin Q devuelve los primeros resultados.</summary>
public sealed record BusquedaCatalogoRequest
{
    [StringLength(100)] public string? Q { get; init; }
    /// <summary>Máximo de resultados (1 a 50 en aeropuertos, 1 a 100 en aviones).</summary>
    [Range(1, 100)] public int Limite { get; init; } = 20;
}

/// <summary>Aeropuerto activo en formato mínimo para selectores. No incluye metadatos de importación.</summary>
public sealed record AeropuertoResumenResponse(int AeropuertoId, string CodigoIata, string? CodigoIcao, string Nombre, string Ciudad, string Pais);

/// <summary>Avión activo en formato mínimo para selectores.</summary>
public sealed record AvionResumenResponse(int AvionId, string Modelo, string Matricula, int CapacidadTotal);
