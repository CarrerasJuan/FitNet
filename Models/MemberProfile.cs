namespace FitNet.Models;

public class MemberProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int MembershipId { get; set; }
    public Membership Membership { get; set; } = null!;

    public DateTime? BirthDate { get; set; }
    public int? Height { get; set; } // En centímetros
    public DateTime TrialEndsAt { get; set; } // Límite del trial de 24 horas
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public ICollection<WorkoutAssignment> Assignments { get; set; } = new List<WorkoutAssignment>();
    public ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();
}
