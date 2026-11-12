namespace Vuelos_Backend.Repositories.Catalogos;

internal static class PatronesPostgreSql
{
    // Los caracteres del usuario se buscan literalmente; solo el servidor añade comodines.
    private static string Escapar(string value) => value.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");
    public static string Contiene(string value) => "%" + Escapar(value) + "%";
    public static string Prefijo(string value) => Escapar(value) + "%";
}
