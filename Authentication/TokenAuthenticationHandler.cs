using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Vuelos_Backend.Interface.Autenticacion;
namespace Vuelos_Backend.Authentication;

public sealed class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IAutenticacionService autenticacion)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string NombreEsquema = "Bearer";
    public const string SesionIdClaim = "sesion_id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers.Authorization;
        if (headers.Count == 0)
            return AuthenticateResult.NoResult();
        if (headers.Count != 1 ||
            !AuthenticationHeaderValue.TryParse(headers[0], out var authorization) ||
            !string.Equals(authorization.Scheme, NombreEsquema, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authorization.Parameter))
            return AuthenticateResult.Fail("Credenciales no válidas.");

        var sesion = await autenticacion.ValidarTokenAsync(authorization.Parameter, Context.RequestAborted);
        if (sesion is null)
            return AuthenticateResult.Fail("Credenciales no válidas.");

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, sesion.Rol),
            new Claim(SesionIdClaim, sesion.SesionId.ToString(CultureInfo.InvariantCulture))
        ], NombreEsquema);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), NombreEsquema));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = NombreEsquema;
        await Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
            title: "No autorizado",
            detail: "Se requiere una sesión válida y vigente.").ExecuteAsync(Context);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado").ExecuteAsync(Context);
}
