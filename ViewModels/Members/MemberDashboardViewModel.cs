namespace FitNet.ViewModels.Members;

public class MemberDashboardViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }

    public string PlanName { get; set; } = "Free";
    public bool IsTrialActive { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int TrialHoursRemaining { get; set; }

    public int? Height { get; set; }
    public decimal? CurrentWeight { get; set; }

    // Datos de la rutina activa si tiene plan Gold/Premium
    public string? ActiveWorkoutTitle { get; set; }
    public int ActiveWorkoutExercisesCount { get; set; }
    public string? ActiveWorkoutDifficulty { get; set; }

    // Entrenador asignado si tiene plan Premium
    public string? AssignedTrainerName { get; set; }
    public string? AssignedTrainerSpecialty { get; set; }
    public string? AssignedTrainerAvatar { get; set; }
}
