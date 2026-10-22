namespace FitNet.ViewModels.Admin;

/// <summary>
/// Modelo de vista para el Dashboard Ejecutivo de Métricas de Negocio (Owner).
/// Calculado 100% mediante agregaciones optimizadas en MySQL / EF Core.
/// </summary>
public class BusinessMetricsViewModel
{
    // ==========================================
    // 1. Ingresos Financieros Proyectados
    // ==========================================
    /// <summary>
    /// Total de ingresos mensuales recurrentes (MRR) proyectados por membresías activas.
    /// </summary>
    public decimal ProjectedMonthlyRevenue { get; set; }

    /// <summary>
    /// Total de ingresos semestrales proyectados (con descuento del 20%).
    /// </summary>
    public decimal ProjectedSemiannualRevenue { get; set; }

    /// <summary>
    /// Ingreso anual proyectado (ARR = MRR * 12).
    /// </summary>
    public decimal ProjectedAnnualRevenue => ProjectedMonthlyRevenue * 12;

    /// <summary>
    /// Ingreso promedio por socio de pago activo (ARPU).
    /// </summary>
    public decimal AverageRevenuePerUser => PaidMembersCount > 0 
        ? Math.Round(ProjectedMonthlyRevenue / PaidMembersCount, 2) 
        : 0;

    // ==========================================
    // 2. Distribución de Membresías
    // ==========================================
    public List<PlanDistributionItem> PlanDistribution { get; set; } = new();

    // ==========================================
    // 3. Conversión de Free Trial a Planes Pagos
    // ==========================================
    public int TotalMembers { get; set; }
    public int FreeMembersCount { get; set; }
    public int PaidMembersCount { get; set; }
    public int GoldMembersCount { get; set; }
    public int PremiumMembersCount { get; set; }

    /// <summary>
    /// Tasa de conversión de socios a planes comerciales pagos (Gold + Premium / Total).
    /// </summary>
    public double ConversionRate => TotalMembers > 0 
        ? Math.Round((double)PaidMembersCount / TotalMembers * 100, 2) 
        : 0;

    public int ActiveTrialsCount { get; set; }
    public int ExpiredTrialsCount { get; set; }

    // ==========================================
    // 4. Actividad y Retención de Atletas
    // ==========================================
    public int ActiveAthletesCount { get; set; }
    public int InactiveAthletesCount { get; set; }

    /// <summary>
    /// Porcentaje de retención y vigencia de atletas activos sobre el total.
    /// </summary>
    public double ActiveAthletesPercentage => TotalMembers > 0 
        ? Math.Round((double)ActiveAthletesCount / TotalMembers * 100, 2) 
        : 0;

    // ==========================================
    // 5. Métricas Operativas del Gimnasio
    // ==========================================
    public int TotalTrainersCount { get; set; }
    public int TotalWorkoutPlansCount { get; set; }
    public int ActiveWorkoutAssignmentsCount { get; set; }
    public int TotalProgressRecordsCount { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class PlanDistributionItem
{
    public string PlanName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public double Percentage { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string BadgeColor { get; set; } = "secondary";
}
