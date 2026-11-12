using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vuelos_Backend.Configuration;

namespace Vuelos_Backend.Data;

/// <summary>Permite inspeccionar el modelo con EF sin iniciar la API ni sus workers.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configurationDirectory = Path.GetDirectoryName(typeof(AppDbContext).Assembly.Location)!;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(configurationDirectory, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(configurationDirectory, $"appsettings.{environment}.json"), optional: true)
            // La fábrica es solo para herramientas locales; launchSettings no se carga aquí.
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var options = new DbContextOptionsBuilder<AppDbContext>();
        BaseDatosPostgreSql.Configurar(options, configuration);
        return new AppDbContext(options.Options);
    }
}
