using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Services.Catalogos;

public sealed class ImportacionAvionesService(ICatalogosAdminRepository repository) : IImportacionAvionesService
{
    public const int MaxBytes = 1024 * 1024;
    public const int MaxFilas = 500;
    public const int MaxAsientos = 100000;
    private static readonly string[] Columnas = ["modelo", "codigo_iata", "codigo_icao", "matricula", "capacidad_total",
        "cantidad_primera_clase", "cantidad_segunda_clase", "cantidad_tercera_clase", "letras_por_fila"];
    private static readonly string[] Advertencias = ["Las capacidades se validan por estructura y distribución; no se certifican según el modelo real."];

    public async Task<ImportacionAvionesResponse> ValidarAsync(ImportarAvionesRequest request, CancellationToken ct)
    {
        var datos = await LeerAsync(request, ct);
        return await EvaluarAsync(request, datos.Filas, datos.Errores, ct);
    }

    public async Task<ImportacionAvionesResponse> ImportarAsync(ImportarAvionesRequest request, CancellationToken ct)
    {
        // Leer y limitar el archivo antes de adquirir bloqueos SQL; nunca se guarda en disco.
        var datos = await LeerAsync(request, ct);
        if (datos.Errores.Count > 0) return Resultado("errores_validacion", "Corrija los errores del archivo antes de importar.", datos.Filas, datos.Errores);
        await using var tx = await repository.IniciarTransaccionAsync(ct);
        // El bloqueo administrativo compartido con altas/ediciones protege esta comprobación hasta commit.
        var previa = await EvaluarAsync(request, datos.Filas, datos.Errores, ct);
        if (!previa.PuedeImportar) return previa;
        var creadas = new List<FilaImportacionAvionResponse>(previa.TotalFilas);
        foreach (var fila in previa.Filas)
        {
            ct.ThrowIfCancellationRequested();
            var cantidades = new[] { (request.PrimeraClaseId, fila.CantidadPrimeraClase),
                (request.SegundaClaseId, fila.CantidadSegundaClase), (request.TerceraClaseId, fila.CantidadTerceraClase) };
            var avion = await CreacionAviones.CrearAsync(repository, new CrearAvionRequest
            {
                Modelo = fila.Modelo, Matricula = fila.Matricula, CapacidadTotal = fila.CapacidadTotal,
                LetrasPorFila = fila.LetrasPorFila,
                DistribucionClases = cantidades.Where(c => c.Item2 > 0).Select(c => new DistribucionClaseRequest { CategoriaId = c.Item1, Cantidad = c.Item2 }).ToArray()
            }, ct, new MetadatosAvionResponse(fila.CodigoIata, fila.CodigoIcao, fila.CantidadPrimeraClase,
                fila.CantidadSegundaClase, fila.CantidadTerceraClase, fila.LetrasPorFila, fila.DatosOrigenJson));
            creadas.Add(fila with { MatriculaRegistrada = true, AvionId = avion.AvionId });
            repository.LimpiarSeguimientoImportacion();
        }
        await tx.ConfirmarAsync(ct);
        return Resultado("importado", "Aviones y asientos importados correctamente.", creadas, [], previa.TotalFilas, previa.TotalAsientos);
    }

    private async Task<ImportacionAvionesResponse> EvaluarAsync(ImportarAvionesRequest request,
        IReadOnlyList<FilaImportacionAvionResponse> filas, List<ErrorImportacionAvionResponse> errores, CancellationToken ct)
    {
        if (errores.Count > 0) return Resultado("errores_validacion", "Corrija los errores del archivo antes de importar.", filas, errores);
        var existentes = await repository.ObtenerPorMatriculasAsync(filas.Select(f => f.Matricula).ToArray(), ct);
        var porMatricula = existentes.ToDictionary(a => a.Matricula.Trim(), StringComparer.OrdinalIgnoreCase);
        var vista = filas.Select(f => porMatricula.TryGetValue(f.Matricula, out var avion)
            ? f with { MatriculaRegistrada = true, AvionId = avion.AvionId } : f).ToArray();
        if (vista.All(f => f.MatriculaRegistrada))
            return Resultado("ya_importados", "Estos aviones ya se importaron o sus matrículas ya están registradas. No se creó ningún registro.", vista, []);
        if (vista.Any(f => f.MatriculaRegistrada))
            return Resultado("conflicto_matriculas", "Algunas matrículas ya están registradas. Retire esas filas y vuelva a validar; no se importó ningún avión.", vista,
                vista.Where(f => f.MatriculaRegistrada).Select(f => new ErrorImportacionAvionResponse(f.Fila, "matricula", "La matrícula ya existe, incluso si el avión está inactivo.")).ToArray());

        var ids = new[] { request.PrimeraClaseId, request.SegundaClaseId, request.TerceraClaseId };
        if (ids.Distinct().Count() != 3)
            errores.Add(new(1, "clases", "Seleccione tres categorías distintas para Primera, Segunda y Tercera Clase."));
        foreach (var id in ids.Distinct())
        {
            var categoria = await repository.ObtenerAsync<CategoriaAsiento>(id, ct);
            if (categoria is null || !categoria.Activo) errores.Add(new(1, "clases", $"La categoría {id} no existe o está inactiva."));
        }
        return errores.Count == 0 ? Resultado("valido", "Archivo válido. Revise la vista previa y confirme la importación.", vista, [])
            : Resultado("errores_validacion", "Corrija la selección de clases antes de importar.", vista, errores);
    }

    private static async Task<(IReadOnlyList<FilaImportacionAvionResponse> Filas, List<ErrorImportacionAvionResponse> Errores)> LeerAsync(ImportarAvionesRequest request, CancellationToken ct)
    {
        var validaciones = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validaciones, true)) throw ArchivoInvalido("Seleccione el archivo y los tres IDs positivos de las clases.");
        var archivo = request.Archivo;
        if (!string.Equals(Path.GetExtension(archivo.FileName), ".csv", StringComparison.OrdinalIgnoreCase)) throw ArchivoInvalido("El archivo debe ser CSV UTF-8 separado por punto y coma; no se admite Excel .xlsx.");
        if (archivo.Length <= 0 || archivo.Length > MaxBytes) throw ArchivoInvalido("El archivo debe contener datos y no superar 1 MiB.");
        using var buffer = new MemoryStream();
        await using (var entrada = archivo.OpenReadStream())
        {
            var bloque = new byte[8192];
            int leidos;
            while ((leidos = await entrada.ReadAsync(bloque, ct)) > 0)
            {
                if (buffer.Length + leidos > MaxBytes) throw ArchivoInvalido("El archivo supera 1 MiB.");
                buffer.Write(bloque, 0, leidos);
            }
        }
        string texto;
        try { texto = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { throw ArchivoInvalido("Guarde el archivo como CSV UTF-8."); }
        using var parser = new TextFieldParser(new StringReader(texto)) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(";");
        try
        {
            var encabezados = parser.ReadFields()?.Select(s => s.Trim().ToLowerInvariant()).ToArray() ?? [];
            if (encabezados.Length != Columnas.Length || encabezados.Distinct().Count() != Columnas.Length || Columnas.Except(encabezados).Any())
                throw ArchivoInvalido("Se requieren exactamente estos encabezados, sin repetidos: " + string.Join(";", Columnas));
            var indices = encabezados.Select((nombre, i) => (nombre, i)).ToDictionary(p => p.nombre, p => p.i);
            var filas = new List<FilaImportacionAvionResponse>();
            var errores = new List<ErrorImportacionAvionResponse>();
            var matriculas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var registros = 0;
            while (!parser.EndOfData)
            {
                ct.ThrowIfCancellationRequested();
                var numero = checked((int)parser.LineNumber);
                var campos = parser.ReadFields() ?? [];
                if (++registros > MaxFilas) throw ArchivoInvalido("El archivo supera el máximo de 500 aviones.");
                if (campos.Length != Columnas.Length)
                {
                    errores.Add(new(numero, "fila", "La fila debe contener exactamente nueve columnas."));
                    filas.Add(new(numero, "", "", 0, 0, 0, 0, "", "", "", false, null));
                    continue;
                }
                string Campo(string nombre) => campos[indices[nombre]].Trim();
                void Error(string campo, string mensaje) => errores.Add(new(numero, campo, mensaje));
                int Entero(string nombre, int min, int max)
                {
                    if (!int.TryParse(Campo(nombre), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < min || value > max)
                    { Error(nombre, $"Debe ser un entero entre {min} y {max}."); return 0; }
                    return value;
                }
                var modelo = Campo("modelo");
                var matricula = Campo("matricula").ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(modelo) || modelo.Length > 100 || modelo.Any(char.IsControl)) Error("modelo", "Indique un modelo de 1 a 100 caracteres, sin controles ni saltos de línea.");
                if (string.IsNullOrWhiteSpace(matricula) || matricula.Length > 50 || matricula.Any(char.IsControl)) Error("matricula", "Indique una matrícula de 1 a 50 caracteres, sin controles ni saltos de línea.");
                if (matriculas.TryGetValue(matricula, out var anterior)) Error("matricula", $"Matrícula repetida; también aparece en la fila {anterior}.");
                else matriculas.Add(matricula, numero);
                var capacidad = Entero("capacidad_total", 1, 1000);
                var primera = Entero("cantidad_primera_clase", 0, 1000);
                var segunda = Entero("cantidad_segunda_clase", 0, 1000);
                var tercera = Entero("cantidad_tercera_clase", 0, 1000);
                if (primera + segunda + tercera != capacidad) Error("capacidad_total", "Las cantidades por clase deben sumar exactamente la capacidad total.");
                var letras = Campo("letras_por_fila").ToUpperInvariant();
                if (letras.Length > 26 || letras.Any(c => c is < 'A' or > 'Z') || letras.Distinct().Count() != letras.Length)
                    Error("letras_por_fila", "Use hasta 26 letras A-Z sin repetir, o vacío para números consecutivos.");
                var iata = Campo("codigo_iata").ToUpperInvariant(); var icao = Campo("codigo_icao").ToUpperInvariant();
                if (iata.Length > 3 || icao.Length > 4 || iata.Any(char.IsControl) || icao.Any(char.IsControl)) Error("codigos", "IATA admite hasta 3 caracteres e ICAO hasta 4, sin controles.");
                var original = JsonSerializer.Serialize(encabezados.Select((campo, i) => (campo, valor: campos[i])).ToDictionary(p => p.campo, p => p.valor));
                filas.Add(new(numero, modelo, matricula, capacidad, primera, segunda, tercera, letras, iata, icao, false, null, original));
            }
            if (registros == 0) errores.Add(new(1, "archivo", "El archivo no contiene aviones."));
            if (filas.Sum(f => f.CapacidadTotal) > MaxAsientos) errores.Add(new(1, "capacidad_total", "La carga supera el máximo de 100000 asientos; divida el archivo."));
            return (filas, errores);
        }
        catch (MalformedLineException) { throw ArchivoInvalido("CSV mal formado: revise delimitadores y comillas."); }
    }

    private static ImportacionAvionesResponse Resultado(string estado, string mensaje, IReadOnlyList<FilaImportacionAvionResponse> filas,
        IReadOnlyList<ErrorImportacionAvionResponse> errores, int avionesImportados = 0, int asientosImportados = 0) =>
        new(estado, mensaje, estado == "valido", filas.Count, filas.Sum(f => f.CapacidadTotal), avionesImportados, asientosImportados, filas, errores, Advertencias);
    private static CatalogoOperacionException ArchivoInvalido(string mensaje) => new(ErrorCatalogo.SolicitudInvalida, "archivo_aviones_invalido", mensaje);
}
