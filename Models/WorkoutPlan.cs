namespace FitNet.Models;

public class WorkoutPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TrainerId { get; set; }
    public ApplicationUser? Trainer { get; set; }
    public bool IsGeneral { get; set; } = false; // true = General del club (Gold), false = personalizada (Premium)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
    public ICollection<WorkoutAssignment> Assignments { get; set; } = new List<WorkoutAssignment>();
}
