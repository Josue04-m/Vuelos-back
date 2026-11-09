namespace Vuelos_Backend.Models.Pasajeros;

public enum ErrorConsultaCedula { SolicitudInvalida, ProveedorNoDisponible, SinResultado, TiempoAgotado }

public sealed class ConsultaCedulaException(ErrorConsultaCedula error) : Exception(MensajePara(error))
{
    public ErrorConsultaCedula Error { get; } = error;
    public string Codigo => Error switch
    {
        ErrorConsultaCedula.SolicitudInvalida => "cedula_formato_invalido",
        ErrorConsultaCedula.SinResultado => "cedula_sin_resultados",
        ErrorConsultaCedula.TiempoAgotado => "consulta_cedula_tiempo_agotado",
        _ => "proveedor_cedula_no_disponible"
    };
    private static string MensajePara(ErrorConsultaCedula error) => error switch
    {
        ErrorConsultaCedula.SolicitudInvalida => "Ingrese una cédula de diez dígitos.",
        ErrorConsultaCedula.SinResultado => "No se encontraron datos. Puede ingresarlos manualmente.",
        ErrorConsultaCedula.TiempoAgotado => "La consulta superó el tiempo de espera. Puede ingresar los datos manualmente.",
        _ => "No se pudo completar la consulta. Puede ingresar los datos manualmente."
    };
}
