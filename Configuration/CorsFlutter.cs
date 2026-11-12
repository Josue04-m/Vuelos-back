namespace Vuelos_Backend.Configuration;

public static class CorsFlutter
{
    public const string Politica = "FlutterWeb";
    public const string OrigenDesarrollo = "http://localhost:62226";

    public static void AgregarCorsFlutter(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var configurados = configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>();
        var origenes = configurados ?? (environment.IsDevelopment() ? [OrigenDesarrollo] : Array.Empty<string>());
        var normalizados = origenes.Select(origen =>
        {
            if (!Uri.TryCreate(origen, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
                uri.AbsolutePath != "/" || uri.Query != "" || uri.Fragment != "" || uri.UserInfo != "" || uri.Host.Contains('*'))
                throw new InvalidOperationException("Cors:OrigenesPermitidos debe contener orígenes HTTP/HTTPS explícitos, sin rutas ni comodines.");
            return uri.GetLeftPart(UriPartial.Authority);
        }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        services.AddCors(options => options.AddPolicy(Politica, policy =>
            policy.WithOrigins(normalizados).AllowAnyHeader().AllowAnyMethod()));
    }

    public static void UsarCorsFlutter(this WebApplication app)
    {
        // Activar solo cuando el proxy externo, como Render, impone HTTPS.
        if (!app.Environment.IsDevelopment() && !app.Configuration.GetValue<bool>("Hosting:HttpsEnProxy"))
            app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors(Politica);
    }
}
