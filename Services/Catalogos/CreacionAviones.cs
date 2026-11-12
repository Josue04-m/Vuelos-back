using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Models.Asientos;
using Vuelos_Backend.Models.Aviones;
using Vuelos_Backend.Models.Catalogos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Services.Catalogos;

/// <summary>Genera aviones y asientos para la carga CSV dentro de la transaccion administrativa.</summary>
internal static class CreacionAviones
{
    internal static async Task<Avion> CrearAsync(ICatalogosAdminRepository repository, DatosCreacionAvion request, CancellationToken ct, MetadatosAvionResponse? metadatos = null)
    {
        Validar(request);
        var letras = request.LetrasPorFila?.ToUpperInvariant() ?? "";
        if (letras.Distinct().Count() != letras.Length) throw Invalido("Las letras de cada fila no pueden repetirse.");
        var distribucion = request.DistribucionClases ?? throw Invalido("Indique la distribución de clases del avión.");
        if (distribucion.Count == 0 || distribucion.Any(d => d is null)) throw Invalido("La distribución no puede estar vacía ni contener elementos nulos.");
        foreach (var clase in distribucion) Validar(clase);
        if (distribucion.Select(d => d.CategoriaId).Distinct().Count() != distribucion.Count || distribucion.Sum(d => (long)d.Cantidad) != request.CapacidadTotal)
            throw Invalido("Las clases no pueden repetirse y sus cantidades deben sumar la capacidad exacta del avión.");
        foreach (var clase in distribucion)
        {
            var categoria = await repository.ObtenerAsync<CategoriaAsiento>(clase.CategoriaId, ct)
                ?? throw new CatalogoOperacionException(ErrorCatalogo.NoEncontrado, "catalogo_no_encontrado", "Elemento del catálogo no encontrado.");
            if (!categoria.Activo) throw Invalido("Todas las categorías de la distribución deben estar activas.");
        }
        if (metadatos is not null &&
            (metadatos.CantidadPrimeraClase is not int primera || metadatos.CantidadSegundaClase is not int segunda || metadatos.CantidadTerceraClase is not int tercera ||
             primera < 0 || segunda < 0 || tercera < 0 || (long)primera + segunda + tercera != request.CapacidadTotal || metadatos.LetrasPorFila != letras ||
             metadatos.CodigoIata?.Length > 3 || metadatos.CodigoIcao?.Length > 4))
            throw Invalido("Los metadatos importados deben coincidir con la capacidad y las letras del avión.");
        static string? Codigo(string? c) => string.IsNullOrWhiteSpace(c) || c == "\\N" ? null : c.Trim().ToUpperInvariant();
        var avion = new Avion { Modelo = request.Modelo.Trim(), Matricula = request.Matricula.Trim().ToUpperInvariant(), CapacidadTotal = request.CapacidadTotal,
            CodigoIata = Codigo(metadatos?.CodigoIata), CodigoIcao = Codigo(metadatos?.CodigoIcao),
            CantidadPrimeraClase = metadatos?.CantidadPrimeraClase, CantidadSegundaClase = metadatos?.CantidadSegundaClase,
            CantidadTerceraClase = metadatos?.CantidadTerceraClase, LetrasPorFila = letras, DatosOrigenJson = metadatos?.DatosOrigenJson };
        await repository.CrearAsync(avion, ct);
        var asientos = new List<AsientoFisico>(request.CapacidadTotal);
        var i = 0;
        foreach (var clase in distribucion)
        for (var n = 0; n < clase.Cantidad; n++, i++)
        {
            var codigo = letras.Length == 0 ? (i + 1).ToString(CultureInfo.InvariantCulture)
                : (i / letras.Length + 1).ToString(CultureInfo.InvariantCulture) + letras[i % letras.Length];
            asientos.Add(new AsientoFisico { AvionId = avion.AvionId, CategoriaId = clase.CategoriaId, CodigoAsiento = codigo });
        }
        await repository.CrearAsientosAsync(asientos, ct);
        var leido = await repository.ObtenerAsync<Avion>(avion.AvionId, ct) ?? throw Invalido("No se pudo consultar el avión creado.");
        if (leido.Modelo != avion.Modelo || leido.Matricula != avion.Matricula || leido.CapacidadTotal != avion.CapacidadTotal ||
            leido.CodigoIata != avion.CodigoIata || leido.CodigoIcao != avion.CodigoIcao || leido.CantidadPrimeraClase != avion.CantidadPrimeraClase ||
            leido.CantidadSegundaClase != avion.CantidadSegundaClase || leido.CantidadTerceraClase != avion.CantidadTerceraClase ||
            leido.LetrasPorFila != avion.LetrasPorFila || leido.DatosOrigenJson != avion.DatosOrigenJson)
            throw new CatalogoOperacionException(ErrorCatalogo.Conflicto, "datos_avion_inconsistentes", "La base no conservó los datos completos del avión. Se revierte toda la carga; revise tipos y codificación.");
        return leido;
    }

    private static void Validar(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true)) throw Invalido(errors[0].ErrorMessage ?? "Solicitud inválida.");
    }
    private static CatalogoOperacionException Invalido(string message) => new(ErrorCatalogo.SolicitudInvalida, "solicitud_catalogo_invalida", message);
}
