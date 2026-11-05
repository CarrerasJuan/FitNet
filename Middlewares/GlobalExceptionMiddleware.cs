using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace FitNet.Middlewares;

/// <summary>
/// Middleware centralizado para el manejo global de excepciones y resiliencia en FitNet.
/// Implementa la especificación RFC 7807 (ProblemDetails) para endpoints de API REST (/api/*)
/// y redirección amigable con trazabilidad para peticiones web MVC.
/// Captura y mapea errores específicos de MySQL (claves foráneas 1451, duplicados 1062, caídas de conexión).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("[GlobalExceptionMiddleware] La respuesta ya comenzó a emitirse; no se puede modificar el payload.");
            return;
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var userName = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anónimo";

        // Mapeo defensivo de la excepción
        var (statusCode, title, detail, errorCode) = MapException(exception);

        // Registro estructurado mediante ILogger
        _logger.LogError(exception,
            "[INCIDENCIA GLOBAL] TraceId: {TraceId} | ErrorCode: {ErrorCode} | HTTP: {StatusCode} | {Method} {Path} | Usuario: {User} | Detalle: {Detail}",
            traceId,
            errorCode,
            statusCode,
            method,
            path,
            userName,
            detail);

        // Determinar si el destino es la API REST o el navegador web MVC
        var isApi = IsApiRequest(context);

        if (isApi)
        {
            await WriteProblemDetailsResponseAsync(context, statusCode, title, detail, errorCode, traceId, exception);
        }
        else
        {
            // Petición MVC: Redirección limpia hacia /Home/Error preservando trazabilidad
            var encodedMsg = Uri.EscapeDataString(detail);
            var redirectUrl = $"/Home/Error?statusCode={statusCode}&message={encodedMsg}";
            context.Response.Redirect(redirectUrl);
        }
    }

    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Si el cliente pide explícitamente JSON y no HTML (ej. llamadas fetch/axios)
        var acceptHeader = context.Request.Headers.Accept.ToString();
        var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        return (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
               !acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase)) || isAjax;
    }

    private async Task WriteProblemDetailsResponseAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string errorCode,
        string traceId,
        Exception exception)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        // Extensiones RFC 7807 para auditoría y trazabilidad
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

        if (_env.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        var json = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Title, string Detail, string ErrorCode) MapException(Exception exception)
    {
        // 1. Detección profunda de excepciones nativas de MySQL
        var mysqlEx = FindInnerException<MySqlException>(exception);
        if (mysqlEx != null)
        {
            return mysqlEx.Number switch
            {
                // ER_ROW_IS_REFERENCED_2: Restricción de clave foránea en eliminación o actualización
                1451 => (
                    StatusCodes.Status409Conflict,
                    "Conflicto de integridad referencial",
                    "No es posible eliminar o alterar el registro porque posee dependencias activas (rutinas, asignaciones de entrenamiento o registros de progreso vinculados).",
                    "MYSQL_1451_FOREIGN_KEY_RESTRICT"
                ),
                // ER_NO_REFERENCED_ROW_2: Clave foránea inexistente
                1452 => (
                    StatusCodes.Status400BadRequest,
                    "Relación de datos inexistente",
                    "La operación hace referencia a una entidad vinculada (socio, ejercicio o plan) que no existe en el sistema.",
                    "MYSQL_1452_FOREIGN_KEY_NO_PARENT"
                ),
                // ER_DUP_ENTRY: Violación de índice único
                1062 => (
                    StatusCodes.Status409Conflict,
                    "Registro duplicado",
                    "Ya existe un registro con información idéntica en el sistema (correo electrónico, código o nombre en conflicto).",
                    "MYSQL_1062_DUPLICATE_ENTRY"
                ),
                // Códigos de conexión MySQL caída / no disponible
                1045 or 2002 or 2003 or 2006 or 0 => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Servicio de base de datos no disponible",
                    "No fue posible establecer conexión con el servidor de base de datos MySQL (FitNetDb en XAMPP). Verifique que el servicio esté en ejecución.",
                    "MYSQL_CONNECTION_UNAVAILABLE"
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Error de base de datos MySQL",
                    $"Ocurrió un error en el motor de base de datos relacional (Código MySQL #{mysqlEx.Number}).",
                    $"MYSQL_ERROR_{mysqlEx.Number}"
                )
            };
        }

        // 2. Concurrencia optimista en Entity Framework Core
        if (exception is DbUpdateConcurrencyException)
        {
            return (
                StatusCodes.Status409Conflict,
                "Conflicto de concurrencia optimista",
                "El registro fue modificado o eliminado simultáneamente por otro usuario. Por favor recargue la pantalla e intente nuevamente.",
                "EF_CONCURRENCY_CONFLICT"
            );
        }

        // 3. Excepciones estándar de seguridad y autorización
        if (exception is UnauthorizedAccessException)
        {
            return (
                StatusCodes.Status403Forbidden,
                "Acceso no autorizado",
                "No posee los privilegios suficientes para realizar esta operación.",
                "AUTH_FORBIDDEN"
            );
        }

        // 4. Recursos no encontrados
        if (exception is KeyNotFoundException or FileNotFoundException)
        {
            return (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "El recurso solicitado no fue hallado.",
                "RESOURCE_NOT_FOUND"
            );
        }

        // 5. Argumentos o estados inválidos
        if (exception is ArgumentException or InvalidOperationException)
        {
            return (
                StatusCodes.Status400BadRequest,
                "Solicitud o parámetro inválido",
                exception.Message,
                "BAD_REQUEST"
            );
        }

        // 6. Error general no tipificado
        return (
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor",
            "Ocurrió un error inesperado al procesar la solicitud. El incidente ha sido registrado por el sistema para su resolución técnica.",
            "INTERNAL_SERVER_ERROR"
        );
    }

    private static T? FindInnerException<T>(Exception? ex) where T : Exception
    {
        var current = ex;
        while (current != null)
        {
            if (current is T matched) return matched;
            current = current.InnerException;
        }
        return null;
    }
}
