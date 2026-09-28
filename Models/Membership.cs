namespace FitNet.Models;

public class Membership
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Free, Gold, Premium
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal SemiannualPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public ICollection<MemberProfile> Members { get; set; } = new List<MemberProfile>();
}
