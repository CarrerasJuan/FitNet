namespace FitNet.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MuscleGroup { get; set; } = string.Empty; // Pecho, Espalda, Piernas, Hombros, Brazos, Core
    public string Difficulty { get; set; } = string.Empty;  // Principiante, Intermedio, Avanzado
    public string? ImagePath { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublic { get; set; } = true;              // Habilitado para socios con trial Free
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
