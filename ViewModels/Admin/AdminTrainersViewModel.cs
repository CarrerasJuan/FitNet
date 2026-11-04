namespace FitNet.ViewModels.Admin;

/// <summary>
/// Modelo de vista para el Staff de Entrenadores visto desde la Administración.
/// </summary>
public class AdminTrainersViewModel
{
    public List<TrainerStaffItemViewModel> Trainers { get; set; } = new();
    public int TotalTrainers { get; set; }
    public int TotalWorkoutsCreated { get; set; }
    public int TotalActiveAthletes { get; set; }
}

public class TrainerStaffItemViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalPlansCount { get; set; }
    public int ActiveAthletesCount { get; set; }
    public string Shift { get; set; } = "Turno Mañana / Tarde";
}
