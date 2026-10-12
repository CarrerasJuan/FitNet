using System.ComponentModel.DataAnnotations;
using FitNet.Models;

namespace FitNet.ViewModels.Members;

public class MemberProgressViewModel
{
    // Historial de pesajes y evolución
    public List<ProgressRecord> Records { get; set; } = new();

    // Métricas analíticas del atleta
    public decimal? LatestWeight { get; set; }
    public decimal? LatestBodyFat { get; set; }
    public decimal? InitialWeight { get; set; }
    public decimal? WeightDifference { get; set; }
    public int TotalCheckins => Records.Count;
    public string PlanName { get; set; } = "Gold";

    // Formulario de nuevo pesaje / registro
    [Required(ErrorMessage = "La fecha del pesaje es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha del Registro")]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [Range(30, 300, ErrorMessage = "El peso debe encontrarse entre 30 y 300 kg.")]
    [Display(Name = "Peso Corporal (kg)")]
    public decimal? Weight { get; set; }

    [Range(3, 60, ErrorMessage = "El porcentaje de grasa debe estar entre 3% y 60%.")]
    [Display(Name = "Grasa Corporal (%)")]
    public decimal? BodyFat { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    [Display(Name = "Notas y Sensaciones de Entrenamiento")]
    public string? Notes { get; set; }

    [Display(Name = "Fotografía de Evolución Corporal")]
    public IFormFile? ProgressPhoto { get; set; }
}
