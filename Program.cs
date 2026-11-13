using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Data;
using Vuelos_Backend.Interface.Autenticacion;
using Vuelos_Backend.Interface.Usuarios;
using Vuelos_Backend.Middleware;
using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Usuarios;
using Vuelos_Backend.Repositories.Autenticacion;
using Vuelos_Backend.Repositories.Usuarios;
using Vuelos_Backend.Services.Autenticacion;
using Vuelos_Backend.Services.Usuarios;
using Vuelos_Backend.Interface.Vuelos;
using Vuelos_Backend.Repositories.Vuelos;
using Vuelos_Backend.Services.Vuelos;
using Vuelos_Backend.Swagger;
using Vuelos_Backend.Interface.Reservas;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Repositories.Reservas;
using Vuelos_Backend.Services.Reservas;
using Vuelos_Backend.Interface.Pasajeros;
using Vuelos_Backend.Services.Pasajeros;
using Vuelos_Backend.Interface.Catalogos;
using Vuelos_Backend.Repositories.Catalogos;
using Vuelos_Backend.Services.Catalogos;
using Vuelos_Backend.Interface.Ventas;
using Vuelos_Backend.Repositories.Ventas;
using Vuelos_Backend.Services.Ventas;
using Vuelos_Backend.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AgregarCorsFlutter(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.Configure<Vuelos_Backend.Models.Pagos.OpcionesPayPal>(builder.Configuration.GetSection("PayPal"));
builder.Services.AddHttpClient<Vuelos_Backend.Interface.Pagos.IPayPalClient, Vuelos_Backend.Services.Pagos.PayPalClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.MaxResponseContentBufferSize = 1024 * 1024;
}).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
builder.Services.AddScoped<Vuelos_Backend.Services.Pagos.PagoPayPalService>();
builder.Services.AddScoped<Vuelos_Backend.Interface.Pagos.IPagoPayPalRepository, Vuelos_Backend.Repositories.Pagos.PagoPayPalRepository>();
builder.Services.AddHostedService<Vuelos_Backend.Services.Pagos.ConciliacionPayPalWorker>();
builder.Services.AddHttpClient<IConsultaCedulaService, ConsultaCedulaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.MaxResponseContentBufferSize = 1024 * 1024;
}).RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = false
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<IVueloRepository, VueloRepository>();
builder.Services.AddScoped<IVueloService, VueloService>();
builder.Services.AddScoped<IVueloAdminRepository, VueloAdminRepository>();
builder.Services.AddScoped<IVueloAdminService, VueloAdminService>();
builder.Services.AddScoped<ICatalogosAdminRepository, CatalogosAdminRepository>();
builder.Services.AddScoped<ICatalogosAdminService, CatalogosAdminService>();
builder.Services.AddScoped<IBusquedaCatalogosRepository, BusquedaCatalogosRepository>();
builder.Services.AddScoped<IAeropuertosPublicosRepository, AeropuertosPublicosRepository>();
builder.Services.AddScoped<IImportacionAvionesService, ImportacionAvionesService>();
builder.Services.AddScoped<IImportacionAeropuertosService, ImportacionAeropuertosService>();
builder.Services.AddScoped<IVentasRepository, VentasRepository>();
builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<ICicloReservaRepository, CicloReservaRepository>();
builder.Services.AddScoped<IExpiracionReservasService, ExpiracionReservasService>();
builder.Services.AddHostedService<LiberacionReservasWorker>();
builder.Services.AddOptions<OpcionesReservas>()
    .Bind(builder.Configuration.GetSection(OpcionesReservas.Seccion))
    .ValidateDataAnnotations()
    .Validate(o => o.PorcentajeIva == decimal.Round(o.PorcentajeIva, 2), "Reservas:PorcentajeIva admite hasta dos decimales.")
    .ValidateOnStart();
builder.Services.AddScoped<ICalculadoraTarifas, CalculadoraTarifas>();
builder.Services.Configure<OpcionesTarifasPasajero>(builder.Configuration.GetSection(OpcionesTarifasPasajero.Seccion));
builder.Services.AddOptions<OpcionesSesion>()
    .Bind(builder.Configuration.GetSection(OpcionesSesion.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddAuthentication(TokenAuthenticationHandler.NombreEsquema)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(
        TokenAuthenticationHandler.NombreEsquema, _ => { });
builder.Services.AddAuthorization(PoliticasAcceso.Configurar);
builder.Services.AddDbContext<AppDbContext>(options =>
    Vuelos_Backend.Configuration.BaseDatosPostgreSql.Configurar(options, builder.Configuration));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(DocumentacionApi.Configurar);

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pacific Nexus Airways v1"));
}

app.UsarCorsFlutter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Comprueba el proceso HTTP; no ejecuta consultas ni modifica la base.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
