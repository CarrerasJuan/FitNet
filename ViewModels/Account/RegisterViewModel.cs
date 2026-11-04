using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.Account;

public class RegisterViewModel
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    [Display(Name = "Nombre")]
    public string FirstName
    {
        get => _firstName;
        set => _firstName = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50, ErrorMessage = "El apellido no puede superar los 50 caracteres.")]
    [Display(Name = "Apellido")]
    public string LastName
    {
        get => _lastName;
        set => _lastName = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
    [Display(Name = "Correo Electrónico")]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(100, 250, ErrorMessage = "La altura debe ser entre 100 y 250 cm.")]
    [Display(Name = "Altura en cm (opcional)")]
    public int? Height { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Nacimiento (opcional)")]
    public DateTime? BirthDate { get; set; }

    public string? PlanInteres { get; set; } = "free";
}
