namespace FitNet.DTOs.Exercises;

/// <summary>
/// Representación completa de un ejercicio para operaciones de consulta y detalle.
/// </summary>
public class ExerciseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MuscleGroup { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Cantidad de rutinas en las que está prescripto este ejercicio.
    /// </summary>
    public int WorkoutsCount { get; set; }

    /// <summary>
    /// Indica si el ejercicio puede eliminarse físicamente (solo si WorkoutsCount == 0).
    /// Si tiene rutinas vinculadas, se debe optar por baja lógica (IsActive = false).
    /// </summary>
    public bool CanBeDeleted => WorkoutsCount == 0;
}
