using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutAssignmentCreateViewModel
{
    [Required(ErrorMessage = "Debes seleccionar una rutina de entrenamiento.")]
    [Display(Name = "Rutina a Asignar")]
    public int WorkoutPlanId { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un alumno para la asignación.")]
    [Display(Name = "Alumno Seleccionado")]
    public int MemberProfileId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Inicio")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Finalización (Opcional)")]
    public DateTime? EndDate { get; set; }

    // Metadatos de Contexto Visual
    public string? PreselectedPlanName { get; set; }
    public bool? PreselectedPlanIsGeneral { get; set; }
    public string? PreselectedMemberName { get; set; }
    public string? PreselectedMemberEmail { get; set; }
    public string? PreselectedMemberPlan { get; set; }
    public string? PreselectedMemberAvatar { get; set; }

    // Listas para Combos / Selects Dinámicos
    public List<WorkoutPlanOptionViewModel> AvailablePlans { get; set; } = new();
    public List<MemberAssignmentOptionViewModel> AvailableMembers { get; set; } = new();
}

public class WorkoutPlanOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsGeneral { get; set; }
    public int ExercisesCount { get; set; }
    public string TrainerName { get; set; } = string.Empty;
}

public class MemberAssignmentOptionViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string? CurrentWorkoutName { get; set; }
}
