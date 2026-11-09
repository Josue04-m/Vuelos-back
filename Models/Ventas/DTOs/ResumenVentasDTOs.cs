using System.ComponentModel.DataAnnotations;

namespace Vuelos_Backend.Models.Ventas.DTOs;

/// <summary>Rango opcional de fechas de compra en UTC; ambos días se incluyen.</summary>
public sealed record FiltroVentasRequest : IValidatableObject
{
    [RegularExpression("^(Sandbox|Live)$")]
    public string Entorno { get; init; } = "Sandbox";
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde > Hasta) yield return new ValidationResult("Hasta debe ser igual o posterior a Desde.", [nameof(Hasta)]);
    }
}

/// <summary>Capturas PayPal confirmadas, separadas por entorno. Importe bruto histórico, sin descontar reembolsos.</summary>
public sealed record ResumenVentasResponse(decimal TotalVentas, long CantidadCompras, string Moneda,
    string Proveedor, string Entorno, bool EsSandbox, DateOnly? Desde, DateOnly? Hasta);

public sealed record AcumuladoVentas(decimal Total, long Cantidad);
