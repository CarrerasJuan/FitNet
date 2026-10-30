namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutPlanDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGeneral { get; set; }
    public DateTime CreatedAt { get; set; }

    // Coach creador
    public string? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public string? TrainerSpecialty { get; set; }
    public string? TrainerEmail { get; set; }
    public string? TrainerAvatar { get; set; }

    // Prescripciones técnicas
    public List<WorkoutPlanExerciseDetailItemViewModel> Exercises { get; set; } = new();

    // Atletas con esta rutina asignada
    public List<WorkoutPlanAssignedAthleteViewModel> AssignedAthletes { get; set; } = new();

    // Permisos
    public bool CanEditOrDelete { get; set; }
}

public class WorkoutPlanExerciseDetailItemViewModel
{
    public int Order { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MuscleGroup { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string? VideoUrl { get; set; }
    public int Sets { get; set; }
    public int Repetitions { get; set; }
    public int RestSeconds { get; set; }
    public string? Notes { get; set; }
}

public class WorkoutPlanAssignedAthleteViewModel
{
    public int AssignmentId { get; set; }
    public int MemberProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public bool IsActive { get; set; }
}
