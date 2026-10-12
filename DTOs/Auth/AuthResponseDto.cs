namespace FitNet.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserSummaryDto User { get; set; } = null!;
}

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string Role { get; set; } = string.Empty;
    public string PlanName { get; set; } = "Free";
    public bool IsTrialActive { get; set; }
}
