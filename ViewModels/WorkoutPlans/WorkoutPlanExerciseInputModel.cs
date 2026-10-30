using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutPlanExerciseInputModel
{
    [Required(ErrorMessage = "El identificador del ejercicio es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "Identificador de ejercicio inválido.")]
    public int ExerciseId { get; set; }

    public string? ExerciseName { get; set; }
    public string? MuscleGroup { get; set; }
    public string? Difficulty { get; set; }
    public string? ImagePath { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "El orden debe ser mayor a 0.")]
    public int Order { get; set; } = 1;

    [Required(ErrorMessage = "Las series son obligatorias.")]
    [Range(1, 30, ErrorMessage = "Las series deben estar entre 1 y 30.")]
    public int Sets { get; set; } = 3;

    [Required(ErrorMessage = "Las repeticiones son obligatorias.")]
    [Range(1, 100, ErrorMessage = "Las repeticiones deben estar entre 1 y 100.")]
    public int Repetitions { get; set; } = 10;

    [Required(ErrorMessage = "El tiempo de descanso es obligatorio.")]
    [Range(0, 600, ErrorMessage = "El descanso debe estar entre 0 y 600 segundos.")]
    public int RestSeconds { get; set; } = 60;

    [StringLength(250, ErrorMessage = "Las notas técnicas no pueden superar 250 caracteres.")]
    public string? Notes { get; set; }
}
