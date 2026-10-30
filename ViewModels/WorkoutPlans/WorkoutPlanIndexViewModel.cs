namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutPlanIndexViewModel
{
    public List<WorkoutPlanItemViewModel> Plans { get; set; } = new();

    // Filtros y Búsqueda
    public string Filter { get; set; } = "all"; // all, general, mine
    public string? Search { get; set; }

    // Paginación Server-Side (Skip / Take)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Métricas KPI de Cabecera
    public int TotalPlansCount { get; set; }
    public int GeneralPlansCount { get; set; }
    public int PersonalPlansCount { get; set; }

    // Contexto de Roles
    public bool IsTrainer { get; set; }
    public bool IsAdminOrOwner { get; set; }
}
