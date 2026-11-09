using Microsoft.AspNetCore.Authorization;
namespace Vuelos_Backend.Authentication;

public static class PoliticasAcceso
{
    public const string ClienteOAdministrador = "ClienteOAdministrador";
    public const string SoloAdministrador = "SoloAdministrador";
    public static void Configurar(AuthorizationOptions options)
    {
        var usuarios = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().RequireRole("cliente", "admin").Build();
        options.DefaultPolicy = usuarios;
        options.FallbackPolicy = usuarios; 
        options.AddPolicy(ClienteOAdministrador, usuarios);
        options.AddPolicy(SoloAdministrador, policy =>
            policy.RequireAuthenticatedUser().RequireRole("admin"));
    }
}
