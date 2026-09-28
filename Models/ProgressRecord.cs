namespace FitNet.Models;

public class ProgressRecord
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public MemberProfile Member { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public decimal? Weight { get; set; }  // En kilogramos (ej. 75.50)
    public decimal? BodyFat { get; set; } // Porcentaje de grasa (ej. 14.5)
    public string? Notes { get; set; }
    public string? ImagePath { get; set; } // Ruta a la fotografía de progreso en /uploads/progress/
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
