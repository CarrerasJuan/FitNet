using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.WorkoutPlans;

public class WorkoutPlanCreateViewModel
{
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre de la Rutina")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "La descripción u objetivo no puede superar los 500 caracteres.")]
    [Display(Name = "Objetivo y Descripción")]
    public string? Description { get; set; }

    [Display(Name = "¿Es rutina general del gimnasio?")]
    public bool IsGeneral { get; set; } = false;

    // Asignación directa a alumno (opcional, al venir desde /Trainers/Members o selector)
    public int? MemberProfileId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberEmail { get; set; }
    public string? MemberAvatar { get; set; }

    // Socios elegibles para asignar directamente desde el creador
    public List<MemberAssignmentOptionViewModel> AvailableMembers { get; set; } = new();

    // Prescripción interactiva de ejercicios (mínimo 1)
    public List<WorkoutPlanExerciseInputModel> Exercises { get; set; } = new();

    // Contexto de seguridad
    public bool CanSetGeneral { get; set; } = false; // Solo true para Admin u Owner
}
