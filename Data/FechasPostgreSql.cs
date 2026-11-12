namespace Vuelos_Backend.Data;

internal static class FechasPostgreSql
{
    public static DateTime Normalizar(DateTime date)
    {
        if (date.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Los instantes nuevos o modificados deben ser UTC; no se infiere la zona de fechas históricas.");
        // Npgsql/PostgreSQL almacenan microsegundos, no ticks de 100 ns.
        return new DateTime(date.Ticks - date.Ticks % 10, DateTimeKind.Utc);
    }
}
