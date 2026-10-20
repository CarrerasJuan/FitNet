using FitNet.Models;

namespace FitNet.ViewModels.Trainers;

/// <summary>
/// Modelo de vista para que el entrenador consulte el historial de pesajes y fotos de un alumno.
/// </summary>
public class TrainerStudentProgressViewModel
{
    public int MemberProfileId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentAvatarPath { get; set; }
    public string PlanName { get; set; } = "Premium";
    public int? Height { get; set; }
    public int? Age { get; set; }

    // Rutina activa
    public string? ActiveWorkoutPlanName { get; set; }
    public int? ActiveWorkoutPlanId { get; set; }

    // Registros biométricos y fotos de evolución
    public List<ProgressRecord> Records { get; set; } = new();

    public decimal? LatestWeight { get; set; }
    public decimal? LatestBodyFat { get; set; }
    public decimal? InitialWeight { get; set; }
    public decimal? WeightDifference { get; set; }
}
