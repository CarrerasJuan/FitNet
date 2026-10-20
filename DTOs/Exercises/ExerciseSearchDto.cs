namespace FitNet.DTOs.Exercises;

/// <summary>
/// DTO liviano optimizado para búsquedas rápidas y autocompletados (máximo 10 resultados).
/// Diseñado para alimentar selectores y buscadores dinámicos al armar rutinas.
/// </summary>
public class ExerciseSearchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsPublic { get; set; }
}
