namespace FitNet.Models;

public class WorkoutAssignment
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public MemberProfile Member { get; set; } = null!;

    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public string? AssignedByTrainerId { get; set; }
    public ApplicationUser? AssignedByTrainer { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
