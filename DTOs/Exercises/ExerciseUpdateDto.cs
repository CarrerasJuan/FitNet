using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FitNet.DTOs.Exercises;

/// <summary>
/// Contrato de entrada para la modificación integral de un ejercicio existente.
/// Soporta actualización o reemplazo de imagen técnica vía multipart/form-data.
/// </summary>
public class ExerciseUpdateDto
{
    [Required(ErrorMessage = "El identificador del ejercicio es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador debe ser un entero positivo.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del ejercicio es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El grupo muscular es obligatorio.")]
    [StringLength(80, ErrorMessage = "El grupo muscular no puede superar los 80 caracteres.")]
    public string MuscleGroup { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nivel de dificultad es obligatorio.")]
    [StringLength(50, ErrorMessage = "La dificultad no puede superar los 50 caracteres.")]
    public string Difficulty { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "El enlace de video no puede superar los 255 caracteres.")]
    [Url(ErrorMessage = "El enlace de video debe ser una URL válida (ej. YouTube/Vimeo).")]
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Determina si está visible para socios en período de prueba gratuito (Trial 24h).
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Estado de disponibilidad activa del ejercicio.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Nueva imagen técnica para sustituir la actual (opcional).
    /// </summary>
    public IFormFile? ImageFile { get; set; }

    /// <summary>
    /// Bandera explícita para eliminar la imagen actual sin subir una nueva.
    /// </summary>
    public bool RemoveCurrentImage { get; set; } = false;
}
