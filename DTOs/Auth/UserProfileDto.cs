using FitNet.Services;

namespace FitNet.DTOs.Auth;

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? Height { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Specialty { get; set; }
    public string? Biography { get; set; }
    public MemberAccessStatus AccessStatus { get; set; } = null!;
}
