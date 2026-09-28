namespace FitNet.Models;

public class TrainerProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? Biography { get; set; }
    public string? Specialty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
