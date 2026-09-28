namespace FitNet.Models;

public class WorkoutExercise
{
    public int Id { get; set; }

    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; } = 1;
    public int Sets { get; set; } = 3;
    public int Repetitions { get; set; } = 10;
    public int RestSeconds { get; set; } = 60;
    public string? Notes { get; set; }
}
