using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vuelos_Backend.Authentication;
using Vuelos_Backend.Controllers.Pasajeros;
using Vuelos_Backend.Integrations.EcuadorLegal;
using Vuelos_Backend.Interface.Autenticacion;
using Vuelos_Backend.Interface.Pasajeros;
using Vuelos_Backend.Middleware;
using Vuelos_Backend.Models.Autenticacion;
using Vuelos_Backend.Models.Autenticacion.DTOs;
using Vuelos_Backend.Models.Pasajeros;
using Vuelos_Backend.Models.Pasajeros.DTOs;
using Vuelos_Backend.Services.Pasajeros;

class Checks
{
    static int count;
    static void Check(bool ok) { if (!ok) throw new Exception("Comprobación " + (count + 1)); count++; }
    static async Task Main()
    {
        var handler = new Provider();
        using var http = new HttpClient(handler);
        var service = new ConsultaCedulaService(http);
        var data = await service.ConsultarAsync("0000000000", default);
        Check(data.PrimerNombre == "SOFIA" && data.SegundoNombre == "MARIA" &&
            data.PrimerApellido == "TORRES" && data.SegundoApellido == "PEREZ" && data.RequiereRevision);
        Check(handler.Body == "name=0000000000&tipo=1" && handler.PostCorrecto);
        Check(EcuadorLegalNombreExtractor.Extraer("<td id='name0'>Etiqueta: <strong>MAR&Iacute;A P&Eacute;REZ</strong></td>",
            "0000000000") == "MARÍA PÉREZ");
        Check(EcuadorLegalNombreExtractor.Extraer("<td id='ci0'>1111111111</td><td id='name0'>OTRA PERSONA</td>" +
            "<td id='ci1'>0000000000</td><td id='name1'>PERSONA CORRECTA</td>", "0000000000") == "PERSONA CORRECTA");
        Check(EcuadorLegalNombreExtractor.Extraer("<td id='ci0'>1111111111</td><td id='name0'>OTRA PERSONA</td>",
            "0000000000") is null);
        handler.Html = "<td id='name0'><strong>SOFIA TORRES</strong></td>";
        data = await service.ConsultarAsync("0000000000", default);
        Check(data.PrimerNombre == "SOFIA" && data.PrimerApellido == "TORRES" && data.SegundoNombre == "");
        handler.Html = "<td id='name0'><strong>SOFIA</strong></td>";
        data = await service.ConsultarAsync("0000000000", default);
        Check(data.PrimerNombre == "SOFIA" && data.PrimerApellido == "" && data.RequiereRevision);
        handler.Html = "<p>Sin datos</p>";
        try { await service.ConsultarAsync("0000000000", default); throw new Exception(); }
        catch (ConsultaCedulaException e) { Check(e.Error == ErrorConsultaCedula.SinResultado); }
        handler.Status = HttpStatusCode.InternalServerError;
        try { await service.ConsultarAsync("0000000000", default); throw new Exception(); }
        catch (ConsultaCedulaException e) { Check(e.Error == ErrorConsultaCedula.ProveedorNoDisponible); }
        handler.Status = HttpStatusCode.OK;
        handler.Timeout = true;
        try { await service.ConsultarAsync("0000000000", default); throw new Exception(); }
        catch (ConsultaCedulaException e) { Check(e.Error == ErrorConsultaCedula.TiempoAgotado); }
        handler.Timeout = false;
        handler.Html = "<td id='name0'><strong>SOFIA MARIA TORRES PEREZ</strong></td>";
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(PasajerosController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddSingleton<IConsultaCedulaService>(service);
        builder.Services.AddSingleton<IAutenticacionService, Sessions>();
        builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Bearer", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        Check((await client.GetAsync("/api/pasajeros/consulta-cedula/0000000000")).StatusCode == HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new string('A', 64));
        var response = await client.GetAsync("/api/pasajeros/consulta-cedula/0000000000");
        Check(response.StatusCode == HttpStatusCode.OK &&
            (await response.Content.ReadFromJsonAsync<ConsultaCedulaResponse>())!.SegundoApellido == "PEREZ");
        var before = handler.Calls;
        Check((await client.GetAsync("/api/pasajeros/consulta-cedula/invalida")).StatusCode == HttpStatusCode.BadRequest &&
            handler.Calls == before);
        handler.Html = "sin resultado";
        Check((await client.GetAsync("/api/pasajeros/consulta-cedula/0000000000")).StatusCode == HttpStatusCode.NotFound);
        await app.StopAsync();
        Console.WriteLine("OK: " + count + " comprobaciones. Proveedor simulado; sin cédulas reales ni SQL.");
    }
    class Provider : HttpMessageHandler
    {
        public string Html = "<td id='ci0'>0000000000</td><td id='name0'><strong>SOFIA MARIA TORRES PEREZ</strong></td>";
        public string? Body;
        public bool PostCorrecto, Timeout;
        public int Calls;
        public HttpStatusCode Status = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Body = await request.Content!.ReadAsStringAsync(ct);
            PostCorrecto = request.Method == HttpMethod.Post && request.RequestUri!.AbsoluteUri == ConsultaCedulaService.Endpoint;
            if (Timeout) throw new TaskCanceledException();
            return new HttpResponseMessage(Status) { Content = new StringContent(Html) };
        }
    }
    class Sessions : IAutenticacionService
    {
        public Task<SesionAutenticada?> ValidarTokenAsync(string t, CancellationToken ct) =>
            Task.FromResult<SesionAutenticada?>(new SesionAutenticada(1, 1, "cliente"));
        public Task<LoginResponse?> LoginAsync(LoginRequest r, CancellationToken ct) => Task.FromResult<LoginResponse?>(null);
        public Task LogoutAsync(int s, int u, CancellationToken ct) => Task.CompletedTask;
    }
}
