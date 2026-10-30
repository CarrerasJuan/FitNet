namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutPlanItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGeneral { get; set; }
    public string? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public string? TrainerAvatar { get; set; }
    public int ExercisesCount { get; set; }
    public int ActiveAssignmentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanEditOrDelete { get; set; }
}
