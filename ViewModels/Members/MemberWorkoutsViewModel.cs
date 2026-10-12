using FitNet.Models;

namespace FitNet.ViewModels.Members;

public class MemberWorkoutsViewModel
{
    // Asignaciones personalizadas activas (del entrenador)
    public List<WorkoutAssignment> ActiveAssignments { get; set; } = new();

    // Rutinas generales de la sala de musculación (disponibles para Gold y Premium)
    public List<WorkoutPlan> GeneralPlans { get; set; } = new();

    // Estado del socio
    public string PlanName { get; set; } = "Gold";
    public bool IsPremium => PlanName.Equals("Premium", StringComparison.OrdinalIgnoreCase);
    public int RemainingDays { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
