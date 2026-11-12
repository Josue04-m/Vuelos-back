using Microsoft.EntityFrameworkCore;

namespace Vuelos_Backend.Configuration;

/// <summary>Configuración única para ejecución y herramientas EF; no abre conexiones.</summary>
public static class BaseDatosPostgreSql
{
    public const string Esquema = "pna";
    public const string NombreConexion = "SupabaseConnection";

    public static void Configurar(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString(NombreConexion);
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:SupabaseConnection mediante secretos de usuario o variables de entorno.");

        // Sin fallback a SQL Server ni reintentos automáticos sobre operaciones de pago.
        options.UseNpgsql(connection, postgres =>
            postgres.MigrationsHistoryTable("__EFMigrationsHistory", Esquema));
    }
}
