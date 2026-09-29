using System.ComponentModel.DataAnnotations;

namespace FitNet.ViewModels.Account;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá una nueva contraseña.")]
    [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NewPassword", ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
