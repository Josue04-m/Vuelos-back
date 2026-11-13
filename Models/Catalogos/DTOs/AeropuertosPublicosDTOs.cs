using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Catalogos.DTOs;

/// <summary>Búsqueda pública por nombre de país o código ISO de dos letras.</summary>
public sealed record BuscarAeropuertosPublicosRequest
{
    private string buscar = "";
    [Required, StringLength(100, MinimumLength = 2)]
    public string Buscar { get => buscar; init => buscar = value?.Trim() ?? ""; }
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;
    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;
}

/// <summary>Datos públicos mínimos de un aeropuerto seleccionable.</summary>
public sealed record AeropuertoPublicoResponse(int AeropuertoId, string CodigoIata, string Nombre, string? Ciudad, string Pais);

/// <summary>Página de aeropuertos; los totales incluyen todas las coincidencias.</summary>
public sealed record PaginaAeropuertosPublicosResponse(IReadOnlyList<AeropuertoPublicoResponse> Elementos,
    int Pagina, int TamanoPagina, int Total, int TotalPaginas);
