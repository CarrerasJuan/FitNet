namespace FitNet.ViewModels.Trainers;

/// <summary>
/// Modelo de vista para el panel de alumnos asignados al entrenador con paginado server-side.
/// </summary>
public class TrainerMembersViewModel
{
    public List<TrainerStudentItemViewModel> Students { get; set; } = new();

    // Filtros y Búsqueda
    public string? Search { get; set; }

    // Paginación Server-Side (Skip / Take en MySQL)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Métricas del Entrenador
    public string TrainerName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int TotalAssignedCount { get; set; }
    public int ActiveRoutinesCount { get; set; }
    public int WithoutRoutineCount { get; set; }
}

/// <summary>
/// Representa la ficha resumida de un alumno/socio asignado a cargo del coach.
/// </summary>
public class TrainerStudentItemViewModel
{
    public int MemberProfileId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public string PlanName { get; set; } = "Premium";
    public int? Height { get; set; }
    public int? Age { get; set; }

    // Rutina activa asignada
    public int? ActiveWorkoutPlanId { get; set; }
    public string? ActiveWorkoutPlanName { get; set; }
    public DateTime? AssignmentStartDate { get; set; }
    public int ExercisesInRoutineCount { get; set; }

    // Progreso biométrico más reciente
    public decimal? LatestWeight { get; set; }
    public decimal? LatestBodyFat { get; set; }
    public DateTime? LatestProgressDate { get; set; }
    public int TotalProgressRecords { get; set; }
}
