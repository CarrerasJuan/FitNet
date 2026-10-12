using FitNet.Models;

namespace FitNet.ViewModels.Exercises;

public class ExerciseCatalogViewModel
{
    public List<Exercise> Exercises { get; set; } = new();

    // Filtros activos
    public string? SelectedMuscleGroup { get; set; }
    public string? SelectedDifficulty { get; set; }
    public string? SearchQuery { get; set; }

    // Estado del atleta / membresía
    public bool CanAccessFullLibrary { get; set; }
    public bool IsTrialActive { get; set; }
    public int TrialHoursRemaining { get; set; }
    public string PlanName { get; set; } = "Free";

    // Métricas del catálogo
    public int TotalExercisesCount { get; set; }
    public int PublicExercisesCount { get; set; }
    public int ExclusiveExercisesCount { get; set; }

    // Opciones para filtros
    public static readonly string[] MuscleGroups = ["Pecho", "Espalda", "Piernas", "Hombros", "Brazos", "Core"];
    public static readonly string[] Difficulties = ["Principiante", "Intermedio", "Avanzado"];
}
