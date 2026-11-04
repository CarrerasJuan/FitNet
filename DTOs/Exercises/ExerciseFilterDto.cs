namespace FitNet.DTOs.Exercises;

/// <summary>
/// Parámetros de consulta y filtrado server-side para el endpoint GET /api/exercises.
/// </summary>
public class ExerciseFilterDto
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _page = 1;
    private string? _search;
    private string? _muscleGroup;
    private string? _difficulty;
    private string? _sortBy = "name";

    /// <summary>
    /// Número de página (1-indexed).
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Cantidad de elementos por página (por defecto 10, máximo 50).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    /// <summary>
    /// Búsqueda textual parcial sobre nombre o descripción.
    /// </summary>
    public string? Search
    {
        get => _search;
        set => _search = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Filtro exacto por grupo muscular (Pecho, Espalda, Piernas, Hombros, Brazos, Core).
    /// </summary>
    public string? MuscleGroup
    {
        get => _muscleGroup;
        set => _muscleGroup = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Filtro exacto por dificultad (Principiante, Intermedio, Avanzado).
    /// </summary>
    public string? Difficulty
    {
        get => _difficulty;
        set => _difficulty = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Filtro por visibilidad pública / trial.
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Filtro por estado activo / baja lógica.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Campo por el cual ordenar: "name", "musclegroup", "difficulty", "createdat".
    /// </summary>
    public string? SortBy
    {
        get => _sortBy;
        set => _sortBy = string.IsNullOrWhiteSpace(value) ? "name" : value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Dirección del ordenamiento (true = descendente, false = ascendente).
    /// </summary>
    public bool SortDesc { get; set; } = false;
}
