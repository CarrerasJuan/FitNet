using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FitNet.Models;

namespace FitNet.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null, string? message = null)
    {
        var code = statusCode ?? HttpContext.Response.StatusCode;
        if (code < 400 || code > 599)
        {
            code = 500;
        }

        var (defaultTitle, defaultMsg) = code switch
        {
            400 => ("Petición Incorrecta", "La solicitud no pudo procesarse debido a datos o parámetros inválidos."),
            401 => ("No Autenticado", "Debe iniciar sesión para acceder al recurso solicitado."),
            403 => ("Acceso Denegado", "No posee los privilegios suficientes para acceder a esta sección."),
            404 => ("Página No Encontrada", "La ruta solicitada no existe o fue dada de baja."),
            409 => ("Conflicto de Datos", "No se pudo completar la operación debido a un conflicto con otros registros activos del sistema."),
            503 => ("Servicio No Disponible", "El servidor o la base de datos MySQL (FitNetDb) se encuentran temporalmente inaccesibles."),
            _ => ("Error del Servidor", "Ocurrió un error inesperado al procesar la solicitud. El incidente fue registrado por el sistema para su resolución.")
        };

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code,
            Title = defaultTitle,
            ErrorMessage = !string.IsNullOrWhiteSpace(message) ? message : defaultMsg,
            Path = HttpContext.Request.Path
        };

        return View(model);
    }
}
