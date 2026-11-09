using System.Net;
using System.Text.RegularExpressions;
namespace Vuelos_Backend.Integrations.EcuadorLegal;

public static partial class EcuadorLegalNombreExtractor
{
    public static string? Extraer(string html, string cedula)
    {
        try
        {
            var campos = new Dictionary<string, string>();
            foreach (Match celda in Celdas().Matches(html))
            {
                var id = Identificador().Match(celda.Groups["atributos"].Value).Groups["id"].Value;
                if (id.Length == 0) continue;
                var contenido = celda.Groups["texto"].Value;
                var strong = Resaltado().Match(contenido);
                if (id.StartsWith("name", StringComparison.Ordinal) && strong.Success)
                    contenido = strong.Groups["texto"].Value;
                campos[id] = campos.GetValueOrDefault(id, "") +
                    WebUtility.HtmlDecode(Etiquetas().Replace(contenido, ""));
            }
            foreach (var (id, texto) in campos)
                if (CedulaId().IsMatch(id) && texto.Trim() == cedula &&
                    campos.TryGetValue("name" + id[2..], out var nombre))
                {
                    nombre = Limpiar(nombre);
                    if (nombre.Length > 0) return nombre;
                }
            // También admite el fragmento del ejemplo C#: solo td id="name0".
            if (!campos.ContainsKey("ci0") && campos.TryGetValue("name0", out var primero))
                return Limpiar(primero) is { Length: > 0 } limpio ? limpio : null;
            return null;
        }
        catch (RegexMatchTimeoutException) { return null; }
    }

    private static string Limpiar(string nombre) => Espacios().Replace(nombre, " ").Trim();

    [GeneratedRegex(@"<td\b(?<atributos>(?:[^""'<>]|""[^""]*""|'[^']*')*)>(?<texto>.*?)</td\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 500)]
    private static partial Regex Celdas();
    [GeneratedRegex(@"(?:^|\s)id\s*=\s*(?:""(?<id>[^""]+)""|'(?<id>[^']+)'|(?<id>[^\s>]+))", RegexOptions.IgnoreCase, 100)]
    private static partial Regex Identificador();
    [GeneratedRegex(@"\Aci[0-9]+\z", RegexOptions.None, 100)]
    private static partial Regex CedulaId();
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline, 100)]
    private static partial Regex Etiquetas();
    [GeneratedRegex(@"<strong\b[^>]*>(?<texto>.*?)</strong\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, 100)]
    private static partial Regex Resaltado();
    [GeneratedRegex(@"\s+", RegexOptions.None, 100)]
    private static partial Regex Espacios();
}
