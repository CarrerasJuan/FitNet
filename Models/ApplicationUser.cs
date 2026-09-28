using Microsoft.AspNetCore.Identity;

namespace FitNet.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Propiedades de navegación
    public MemberProfile? MemberProfile { get; set; }
    public TrainerProfile? TrainerProfile { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
