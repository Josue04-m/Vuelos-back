using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Services.Catalogos;

public sealed class ImportacionAeropuertosService(ICatalogosAdminRepository repository) : IImportacionAeropuertosService
{
    public const int MaxBytes = 20 * 1024 * 1024;
    public const int MaxRegistros = 150000;
    public const int MaxImportables = 20000;
    private static readonly string[] Requeridas = ["type", "name", "municipality", "iso_country", "iata_code", "scheduled_service"];

    public async Task<ImportacionAeropuertosResponse> ValidarAsync(ImportarAeropuertosRequest request, CancellationToken ct)
    {
        var datos = await LeerAsync(request, ct);
        return await EvaluarAsync(request, datos, ct);
    }

    public async Task<ImportacionAeropuertosResponse> ImportarAsync(ImportarAeropuertosRequest request, CancellationToken ct)
    {
        var datos = await LeerAsync(request, ct);
        if (datos.TotalErrores > 0) return Resultado(request, datos, "errores_validacion", "Corrija los errores o autorice omitir los registros incompletos.");
        await using var tx = await repository.IniciarTransaccionAsync(ct);
        var vista = await EvaluarAsync(request, datos, ct);
        if (!vista.PuedeImportar) return vista;
        var nuevos = datos.Filas.Select(MetadatosAeropuerto.Crear).ToArray();
        await repository.CrearAeropuertosAsync(nuevos, ct);
        await tx.ConfirmarAsync(ct);
        var porCodigo = nuevos.ToDictionary(a => a.CodigoIata, StringComparer.OrdinalIgnoreCase);
        datos.Filas = datos.Filas.Select(f => f with { CodigoRegistrado = true, AeropuertoId = porCodigo[f.CodigoIata].AeropuertoId }).ToList();
        return Resultado(request, datos, "importado", "Aeropuertos importados correctamente.", nuevos.Length);
    }

    private async Task<ImportacionAeropuertosResponse> EvaluarAsync(ImportarAeropuertosRequest request, Datos datos, CancellationToken ct)
    {
        if (datos.TotalErrores > 0) return Resultado(request, datos, "errores_validacion", "Corrija los errores o autorice omitir los registros incompletos.");
        var existentes = await repository.ObtenerPorCodigosIataAsync(datos.Filas.Select(f => f.CodigoIata).ToArray(), ct);
        var porCodigo = existentes.ToDictionary(a => a.CodigoIata.Trim(), StringComparer.OrdinalIgnoreCase);
        datos.Filas = datos.Filas.Select(f => porCodigo.TryGetValue(f.CodigoIata, out var a) ? f with { CodigoRegistrado = true, AeropuertoId = a.AeropuertoId } : f).ToList();
        if (datos.Filas.All(f => f.CodigoRegistrado)) return Resultado(request, datos, "ya_importados", "Estos aeropuertos ya se importaron o sus códigos IATA ya están registrados. No se creó ningún registro.");
        if (datos.Filas.Any(f => f.CodigoRegistrado))
        {
            foreach (var f in datos.Filas.Where(f => f.CodigoRegistrado)) datos.Error(f.Fila, "iata_code", "El código IATA ya está registrado, incluso si el aeropuerto está inactivo.");
            return Resultado(request, datos, "conflicto_codigos", "Algunos códigos IATA ya existen. Retire esas filas y vuelva a validar; no se importó ningún aeropuerto.");
        }
        return Resultado(request, datos, "valido", "Revise la vista previa y el resumen de omisiones antes de confirmar.");
    }

    private static async Task<Datos> LeerAsync(ImportarAeropuertosRequest request, CancellationToken ct)
    {
        var validaciones = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validaciones, true)) throw Invalido("Seleccione el archivo y revise filtros y paginación.");
        var archivo = request.Archivo;
        if (!string.Equals(Path.GetExtension(archivo.FileName), ".csv", StringComparison.OrdinalIgnoreCase)) throw Invalido("Use airports.csv en UTF-8; no se admite Excel .xlsx.");
        if (archivo.Length <= 0 || archivo.Length > MaxBytes) throw Invalido("El archivo debe contener datos y no superar 20 MiB.");
        using var memoria = new MemoryStream();
        await using (var stream = archivo.OpenReadStream())
        {
            var bloque = new byte[81920]; int leidos;
            while ((leidos = await stream.ReadAsync(bloque, ct)) > 0)
            {
                if (memoria.Length + leidos > MaxBytes) throw Invalido("El archivo supera 20 MiB.");
                memoria.Write(bloque, 0, leidos);
            }
        }
        string texto;
        try { texto = new UTF8Encoding(false, true).GetString(memoria.ToArray()).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { throw Invalido("Guarde el CSV con codificación UTF-8."); }
        using var parser = new TextFieldParser(new StringReader(texto)) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(",");
        var datos = new Datos();
        var codigos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var encabezados = parser.ReadFields()?.Select(s => s.Trim().ToLowerInvariant()).ToArray() ?? [];
            if (encabezados.Any(string.IsNullOrWhiteSpace) || encabezados.Distinct().Count() != encabezados.Length || Requeridas.Except(encabezados).Any())
                throw Invalido("CSV separado por comas con encabezados únicos; se requieren: " + string.Join(",", Requeridas));
            var indices = encabezados.Select((s, i) => (s, i)).ToDictionary(p => p.s, p => p.i);
            while (!parser.EndOfData)
            {
                ct.ThrowIfCancellationRequested();
                var fila = checked((int)parser.LineNumber);
                var valores = parser.ReadFields() ?? [];
                if (++datos.TotalRegistros > MaxRegistros) throw Invalido("El archivo supera 150000 registros.");
                if (valores.Length != encabezados.Length) { datos.Error(fila, "fila", "La cantidad de columnas no coincide con los encabezados."); continue; }
                string Campo(string nombre) => valores[indices[nombre]].Trim();
                if (Campo("type") is not ("small_airport" or "medium_airport" or "large_airport")) { datos.Omitir("tipo_no_admitido"); continue; }
                if (request.SoloServicioRegular && !string.Equals(Campo("scheduled_service"), "yes", StringComparison.OrdinalIgnoreCase)) { datos.Omitir("sin_servicio_regular"); continue; }
                var pais = Campo("iso_country").ToUpperInvariant();
                if (!string.IsNullOrEmpty(request.PaisIso) && !string.Equals(pais, request.PaisIso, StringComparison.OrdinalIgnoreCase)) { datos.Omitir("fuera_del_pais"); continue; }
                var iata = Campo("iata_code").ToUpperInvariant();
                if (iata is "" or "\\N") { datos.Omitir("sin_iata"); continue; }
                var nombre = Campo("name"); var ciudad = Campo("municipality");
                var errores = new List<(string Campo, string Mensaje)>();
                string? Opcional(string campo, int? maximo = null)
                {
                    var valor = indices.TryGetValue(campo, out var indice) ? valores[indice].Trim() : "";
                    if (valor is "" or "\\N") return null;
                    if (maximo.HasValue && valor.Length > maximo.Value) errores.Add((campo, $"Admite hasta {maximo.Value} caracteres."));
                    return valor;
                }
                double? Numero(string campo, double? minimo = null, double? maximo = null)
                {
                    var valor = Opcional(campo);
                    if (valor is null) return null;
                    if (!double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero) || !double.IsFinite(numero))
                    { errores.Add((campo, "Debe ser un número finito con punto decimal, o estar vacío.")); return null; }
                    if (minimo.HasValue && numero < minimo || maximo.HasValue && numero > maximo)
                        errores.Add((campo, $"Debe estar entre {minimo} y {maximo}."));
                    return numero;
                }
                long? idOrigen = null;
                var idTexto = Opcional("id");
                if (idTexto is not null)
                {
                    if (long.TryParse(idTexto, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0) idOrigen = id;
                    else errores.Add(("id", "El ID de origen debe ser un entero positivo."));
                }
                var identificador = Opcional("ident", 50); var tipo = Opcional("type", 32);
                var latitud = Numero("latitude_deg", -90, 90); var longitud = Numero("longitude_deg", -180, 180);
                var elevacion = Numero("elevation_ft"); var continente = Opcional("continent", 2);
                var region = Opcional("iso_region", 20);
                bool? servicio = null;
                var servicioTexto = Opcional("scheduled_service")?.ToLowerInvariant();
                if (servicioTexto == "yes") servicio = true;
                else if (servicioTexto == "no") servicio = false;
                else if (servicioTexto is not null) errores.Add(("scheduled_service", "Use yes, no o vacío."));
                var icao = Opcional("icao_code", 10); var gps = Opcional("gps_code", 20); var local = Opcional("local_code", 20);
                var web = Opcional("home_link"); var wikipedia = Opcional("wikipedia_link"); var keywords = Opcional("keywords");
                if (iata.Length != 3 || iata.Any(c => c is < 'A' or > 'Z')) errores.Add(("iata_code", "IATA debe contener exactamente tres letras A-Z."));
                if (pais.Length != 2 || pais.Any(c => c is < 'A' or > 'Z')) errores.Add(("iso_country", "Indique un país ISO de dos letras."));
                if (string.IsNullOrWhiteSpace(nombre) || nombre == "\\N" || nombre.Length > 150 || nombre.Any(char.IsControl)) errores.Add(("name", "Nombre obligatorio, hasta 150 caracteres, sin controles."));
                if (string.IsNullOrWhiteSpace(ciudad) || ciudad == "\\N" || ciudad.Length > 100 || ciudad.Any(char.IsControl)) errores.Add(("municipality", "Ciudad obligatoria, hasta 100 caracteres, sin controles. No se infiere del nombre."));
                if (errores.Count > 0)
                {
                    if (request.OmitirIncompletos) datos.Omitir("datos_incompletos_o_invalidos");
                    else foreach (var error in errores) datos.Error(fila, error.Campo, error.Mensaje);
                    continue;
                }
                if (codigos.TryGetValue(iata, out var anterior)) { datos.Error(fila, "iata_code", $"Código repetido; también aparece en la fila {anterior}."); continue; }
                codigos.Add(iata, fila);
                // Conservar también los valores CSV originales (sin recortar) y cualquier columna adicional.
                var original = JsonSerializer.Serialize(encabezados.Select((campo, i) => (campo, valor: valores[i])).ToDictionary(p => p.campo, p => p.valor));
                var metadatos = new MetadatosAeropuertoResponse(idOrigen, identificador, tipo, latitud, longitud, elevacion,
                    continente, region, servicio, icao, gps, local, web, wikipedia, keywords, original);
                datos.Filas.Add(new(fila, iata, nombre, ciudad, pais, false, null, metadatos));
                if (datos.Filas.Count > MaxImportables) throw Invalido("La selección supera 20000 aeropuertos; filtre por país o divida el archivo.");
            }
            if (datos.Filas.Count == 0) datos.Error(1, "archivo", "No hay aeropuertos importables con estos filtros.");
            return datos;
        }
        catch (MalformedLineException) { throw Invalido("CSV mal formado: revise delimitadores y comillas."); }
    }

    private static ImportacionAeropuertosResponse Resultado(ImportarAeropuertosRequest request, Datos datos, string estado, string mensaje, int importados = 0) =>
        new(estado, mensaje, estado == "valido", datos.TotalRegistros, datos.Filas.Count, datos.Omisiones.Values.Sum(), datos.TotalErrores,
            datos.TotalErrores > datos.Errores.Count, importados > 0 ? 0 : datos.Filas.Count(f => f.CodigoRegistrado), importados, request.Pagina, request.TamanoPagina,
            (datos.Filas.Count + request.TamanoPagina - 1) / request.TamanoPagina,
            datos.Filas.Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina).ToArray(), datos.Errores, datos.Omisiones);
    private static CatalogoOperacionException Invalido(string mensaje) => new(ErrorCatalogo.SolicitudInvalida, "archivo_aeropuertos_invalido", mensaje);
    private sealed class Datos
    {
        internal int TotalRegistros, TotalErrores;
        internal List<FilaImportacionAeropuertoResponse> Filas = [];
        internal readonly List<ErrorImportacionAeropuertoResponse> Errores = [];
        internal readonly Dictionary<string, int> Omisiones = new(StringComparer.Ordinal);
        internal void Omitir(string motivo) => Omisiones[motivo] = Omisiones.GetValueOrDefault(motivo) + 1;
        internal void Error(int fila, string campo, string mensaje)
        { TotalErrores++; if (Errores.Count < 200) Errores.Add(new(fila, campo, mensaje)); }
    }
}
