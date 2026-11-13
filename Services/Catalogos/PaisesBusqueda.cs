using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Vuelos_Backend.Services.Catalogos;

/// <summary>Nombres españoles CLDR/ICU incorporados como recurso; no consulta servicios externos.</summary>
public static class PaisesBusqueda
{
    private static readonly Dictionary<string, string> Nombres = Cargar();

    private static Dictionary<string, string> Cargar()
    {
        using var stream = typeof(PaisesBusqueda).Assembly.GetManifestResourceStream("Vuelos_Backend.Models.Catalogos.paises.es.json")
            ?? throw new InvalidOperationException("Falta el catálogo de nombres de países.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    private static string Normalizar(string texto) => new string(texto.Trim().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
        .Normalize(NormalizationForm.FormC).ToUpperInvariant();

    public static string[] Resolver(string buscar)
    {
        var texto = Normalizar(buscar);
        if (texto.Length < 2) return [];
        // Un código de dos letras es exacto: EC no debe coincidir con Chequia.
        if (texto.Length == 2 && texto.All(char.IsAsciiLetter)) return [texto];
        return Nombres.Where(p => Normalizar(p.Value).Contains(texto, StringComparison.Ordinal))
            .Select(p => p.Key).ToArray();
    }
}
