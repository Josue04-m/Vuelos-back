using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Vuelos.DTOs;

/// <summary>Consulta administrativa de todos los estados, con países ISO y ordenamiento paginado.</summary>
public sealed record BusquedaVuelosAdminRequest : IValidatableObject
{
    /// <summary>IATA de origen, por ejemplo UIO; opcional, tres letras.</summary>
    [RegularExpression("^[A-Za-z]{3}$")]
    public string? Origen { get; init; }
    /// <summary>IATA de destino, por ejemplo BOG; opcional, tres letras.</summary>
    [RegularExpression("^[A-Za-z]{3}$")]
    public string? Destino { get; init; }
    /// <summary>Día UTC de salida en YYYY-MM-DD; no combinar con el rango.</summary>
    public DateOnly? Fecha { get; init; }
    /// <summary>Primer día UTC de salida incluido. Enviar también fechaHasta.</summary>
    public DateOnly? FechaDesde { get; init; }
    /// <summary>Último día UTC de salida incluido; debe ser igual o posterior a fechaDesde.</summary>
    public DateOnly? FechaHasta { get; init; }
    /// <summary>País ISO del aeropuerto de origen, por ejemplo EC; no distingue mayúsculas.</summary>
    [RegularExpression("^[A-Za-z]{2}$")]
    public string? PaisOrigen { get; init; }
    /// <summary>País ISO del aeropuerto de destino, por ejemplo CO. Puede ser igual al de origen.</summary>
    [RegularExpression("^[A-Za-z]{2}$")]
    public string? PaisDestino { get; init; }
    /// <summary>fechaSalida (predeterminado), numeroVuelo, paisOrigen o paisDestino. Los países se ordenan por ISO.</summary>
    [Required, RegularExpression("^(fechaSalida|numeroVuelo|paisOrigen|paisDestino)$")]
    public string OrdenarPor { get; init; } = "fechaSalida";
    /// <summary>asc (predeterminado) o desc. Los empates conservan un orden estable.</summary>
    [Required, RegularExpression("^(asc|desc)$")]
    public string Direccion { get; init; } = "asc";
    /// <summary>Página desde 1, predeterminada 1.</summary>
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;
    /// <summary>Tamaño de página entre 1 y 100, predeterminado 20.</summary>
    [Range(1, 100)]
    public int TamanoPagina { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        new BusquedaVuelosRequest
        {
            Origen = Origen, Destino = Destino, Fecha = Fecha,
            FechaDesde = FechaDesde, FechaHasta = FechaHasta, Pagina = Pagina, TamanoPagina = TamanoPagina
        }.Validate(validationContext);
}
