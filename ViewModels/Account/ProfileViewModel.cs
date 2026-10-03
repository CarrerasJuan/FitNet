using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.Account;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresá un número de teléfono válido.")]
    [Display(Name = "Teléfono de Contacto")]
    public string? PhoneNumber { get; set; }

    [Range(100, 250, ErrorMessage = "La altura debe estar entre 100 y 250 cm.")]
    [Display(Name = "Altura (cm)")]
    public int? Height { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Nacimiento")]
    public DateTime? BirthDate { get; set; }

    public string? AvatarPath { get; set; }

    [Display(Name = "Subir o Cambiar Foto de Perfil")]
    public IFormFile? AvatarFile { get; set; }

    // Información de Roles y Membresía
    public string Role { get; set; } = "Member";
    public string PlanName { get; set; } = "Free";
    public DateTime? TrialEndsAt { get; set; }
    public bool IsTrialActive => TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
    public int TrialHoursRemaining => TrialEndsAt.HasValue && IsTrialActive 
        ? Math.Max(0, (int)(TrialEndsAt.Value - DateTime.UtcNow).TotalHours) 
        : 0;

    // Entrenador
    public string? Specialty { get; set; }
    public string? Biography { get; set; }
}
