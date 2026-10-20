namespace FitNet.DTOs.Common;

/// <summary>
/// Envoltorio estándar para respuestas uniformes de API REST (éxito y detalle).
/// </summary>
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ApiResponseDto<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponseDto<T> Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Envoltorio estándar sin carga de datos para respuestas de confirmación o error de API REST.
/// </summary>
public class ApiResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ApiResponseDto Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponseDto Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
