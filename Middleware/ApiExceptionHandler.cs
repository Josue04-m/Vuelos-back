using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vuelos_Backend.Models.Common;
using Vuelos_Backend.Models.Reservas;
using Vuelos_Backend.Models.Pasajeros;
using Vuelos_Backend.Models.Vuelos;
using Vuelos_Backend.Models.Catalogos;
namespace Vuelos_Backend.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;
        var duplicado = exception is CorreoDuplicadoException;
        var reserva = exception as ReservaOperacionException;
        var consulta = exception as ConsultaCedulaException;
        var vuelo = exception as VueloAdminOperacionException;
        var catalogo = exception as CatalogoOperacionException;
        if (!duplicado && reserva is null && consulta is null && vuelo is null && catalogo is null)
            logger.LogError("Operación fallida. Tipo: {Tipo}; identificador: {TraceId}",
                exception.GetType().Name, httpContext.TraceIdentifier);
        var problem = new ProblemDetails
        {
            Status = duplicado ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError,
            Title = duplicado ? "Correo no disponible" : "Error interno",
            Detail = duplicado ? "El correo ya está registrado." : "No fue posible completar la operación.",
            Instance = httpContext.Request.Path
        };
        if (reserva is not null)
        {
            problem.Status = reserva.Error switch
            {
                ErrorReserva.SolicitudInvalida => StatusCodes.Status400BadRequest,
                ErrorReserva.NoEncontrada => StatusCodes.Status404NotFound,
                ErrorReserva.Conflicto => StatusCodes.Status409Conflict,
                ErrorReserva.TarifasSinConfigurar => StatusCodes.Status503ServiceUnavailable,
                ErrorReserva.ServicioNoDisponible => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };
            problem.Title = reserva.Error switch
            {
                ErrorReserva.SolicitudInvalida => "Solicitud inválida",
                ErrorReserva.NoEncontrada => "Recurso no encontrado",
                ErrorReserva.Conflicto => "Conflicto de reserva",
                ErrorReserva.TarifasSinConfigurar => "Tarifas no disponibles",
                ErrorReserva.ServicioNoDisponible => "Servicio no disponible",
                _ => "Error interno"
            };
            problem.Detail = reserva.Message;
            problem.Extensions["codigo"] = reserva.Codigo;
            if (reserva.MontoTotalActual is { } monto)
            {
                problem.Extensions["montoTotalActual"] = monto;
                problem.Extensions["moneda"] = "USD";
            }
        }
        if (consulta is not null)
        {
            problem.Status = consulta.Error switch
            {
                ErrorConsultaCedula.SolicitudInvalida => StatusCodes.Status400BadRequest,
                ErrorConsultaCedula.SinResultado => StatusCodes.Status404NotFound,
                ErrorConsultaCedula.TiempoAgotado => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status503ServiceUnavailable
            };
            problem.Title = "Consulta de cédula no disponible";
            problem.Detail = consulta.Message;
            problem.Extensions["codigo"] = consulta.Codigo;
            problem.Extensions["permiteIngresoManual"] = true;
        }
        if (vuelo is not null)
        {
            problem.Status = vuelo.Error switch
            {
                ErrorVueloAdmin.SolicitudInvalida => StatusCodes.Status400BadRequest,
                ErrorVueloAdmin.NoEncontrado => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            problem.Title = "Gestión de vuelos";
            problem.Detail = vuelo.Message;
            problem.Extensions["codigo"] = vuelo.Codigo;
        }
        if (catalogo is not null)
        {
            problem.Status = catalogo.Error switch
            {
                ErrorCatalogo.SolicitudInvalida => StatusCodes.Status400BadRequest,
                ErrorCatalogo.NoEncontrado => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            problem.Title = "Gestión de catálogos";
            problem.Detail = catalogo.Message;
            problem.Extensions["codigo"] = catalogo.Codigo;
        }
        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
