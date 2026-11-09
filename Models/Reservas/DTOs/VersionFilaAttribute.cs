using System.ComponentModel.DataAnnotations;
namespace Vuelos_Backend.Models.Reservas.DTOs;

[AttributeUsage(AttributeTargets.Property)]
public sealed class VersionFilaAttribute : ValidationAttribute
{
    public VersionFilaAttribute() => ErrorMessage = "La versión debe ser Base64 de exactamente 8 bytes.";
    public override bool IsValid(object? value)
    {
        if (value is not string texto) return false;
        Span<byte> bytes = stackalloc byte[8];
        return Convert.TryFromBase64String(texto, bytes, out var escritos) && escritos == 8;
    }
}
