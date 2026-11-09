using Vuelos_Backend.Models.Aeropuertos;
using Vuelos_Backend.Models.Catalogos.DTOs;

namespace Vuelos_Backend.Services.Catalogos;

internal static class MetadatosAeropuerto
{
    internal static MetadatosAeropuertoResponse Desde(Aeropuerto a) => new(a.IdOrigen, a.Identificador, a.Tipo,
        a.Latitud, a.Longitud, a.ElevacionPies, a.Continente, a.RegionIso, a.ServicioRegular, a.CodigoIcao,
        a.CodigoGps, a.CodigoLocal, a.SitioWeb, a.Wikipedia, a.PalabrasClave, a.DatosOrigenJson);

    internal static Aeropuerto Crear(FilaImportacionAeropuertoResponse f)
    {
        var a = new Aeropuerto { CodigoIata = f.CodigoIata, Nombre = f.Nombre, Ciudad = f.Ciudad, Pais = f.Pais };
        if (f.Metadatos is not { } m) return a;
        a.IdOrigen = m.IdOrigen; a.Identificador = m.Identificador; a.Tipo = m.Tipo;
        a.Latitud = m.Latitud; a.Longitud = m.Longitud; a.ElevacionPies = m.ElevacionPies;
        a.Continente = m.Continente; a.RegionIso = m.RegionIso; a.ServicioRegular = m.ServicioRegular;
        a.CodigoIcao = m.CodigoIcao; a.CodigoGps = m.CodigoGps; a.CodigoLocal = m.CodigoLocal;
        a.SitioWeb = m.SitioWeb; a.Wikipedia = m.Wikipedia; a.PalabrasClave = m.PalabrasClave; a.DatosOrigenJson = m.DatosOrigenJson;
        return a;
    }
}
