using FitNet.Data;
using FitNet.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Services;

/// <summary>
/// Implementación optimizada del servicio de métricas de negocio para el rol Owner.
/// Garantiza la ejecución de funciones de agregación (COUNT, SUM, GROUP BY) directamente
/// en el motor de base de datos MySQL a través de Entity Framework Core.
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(ApplicationDbContext context, ILogger<MetricsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BusinessMetricsViewModel> GetBusinessMetricsAsync()
    {
        var now = DateTime.UtcNow;

        _logger.LogInformation("Calculando métricas de negocio para Owner con agregaciones EF Core / MySQL.");

        // 1. Total general de socios registrados
        var totalMembers = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync();

        // 2. Conteo de socios por plan específico (Consultas Count directas)
        var freeCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.Membership.Name == "Free");

        var goldCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.Membership.Name == "Gold");

        var premiumCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.Membership.Name == "Premium");

        var paidMembersCount = goldCount + premiumCount;

        // 3. Distribución de socios por plan (GROUP BY delegado en base de datos)
        var groupedPlans = await _context.MemberProfiles
            .AsNoTracking()
            .GroupBy(m => new { m.Membership.Name, m.Membership.MonthlyPrice })
            .Select(g => new
            {
                PlanName = g.Key.Name,
                MonthlyPrice = g.Key.MonthlyPrice,
                Count = g.Count()
            })
            .ToListAsync();

        var planDistribution = groupedPlans.Select(p => new PlanDistributionItem
        {
            PlanName = p.PlanName,
            MemberCount = p.Count,
            MonthlyPrice = p.MonthlyPrice,
            Percentage = totalMembers > 0 ? Math.Round((double)p.Count / totalMembers * 100, 1) : 0,
            BadgeColor = p.PlanName switch
            {
                "Premium" => "info",
                "Gold" => "warning",
                _ => "secondary"
            }
        })
        .OrderByDescending(p => p.MonthlyPrice)
        .ToList();

        // 4. Ingresos Financieros Proyectados (SUM directo en MySQL para socios activos de pago)
        var projectedMonthlyRevenue = await _context.MemberProfiles
            .AsNoTracking()
            .Where(m => m.Membership.Name != "Free" && m.TrialEndsAt > now)
            .SumAsync(m => m.Membership.MonthlyPrice);

        var projectedSemiannualRevenue = await _context.MemberProfiles
            .AsNoTracking()
            .Where(m => m.Membership.Name != "Free" && m.TrialEndsAt > now)
            .SumAsync(m => m.Membership.SemiannualPrice * 6);

        // 5. Estado del Trial de 24 horas para socios Free
        var activeTrialsCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.Membership.Name == "Free" && m.TrialEndsAt > now);

        var expiredTrialsCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.Membership.Name == "Free" && m.TrialEndsAt <= now);

        // 6. Atletas activos vs. inactivos (Vigencia de membresía o trial)
        var activeAthletesCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.TrialEndsAt > now);

        var inactiveAthletesCount = await _context.MemberProfiles
            .AsNoTracking()
            .CountAsync(m => m.TrialEndsAt <= now);

        // 7. Métricas del Ecosistema Operativo
        var totalTrainersCount = await _context.TrainerProfiles
            .AsNoTracking()
            .CountAsync();

        var totalWorkoutPlansCount = await _context.WorkoutPlans
            .AsNoTracking()
            .CountAsync();

        var activeWorkoutAssignmentsCount = await _context.WorkoutAssignments
            .AsNoTracking()
            .CountAsync(a => a.IsActive);

        var totalProgressRecordsCount = await _context.ProgressRecords
            .AsNoTracking()
            .CountAsync();

        return new BusinessMetricsViewModel
        {
            ProjectedMonthlyRevenue = projectedMonthlyRevenue,
            ProjectedSemiannualRevenue = projectedSemiannualRevenue,
            TotalMembers = totalMembers,
            FreeMembersCount = freeCount,
            GoldMembersCount = goldCount,
            PremiumMembersCount = premiumCount,
            PaidMembersCount = paidMembersCount,
            ActiveTrialsCount = activeTrialsCount,
            ExpiredTrialsCount = expiredTrialsCount,
            ActiveAthletesCount = activeAthletesCount,
            InactiveAthletesCount = inactiveAthletesCount,
            PlanDistribution = planDistribution,
            TotalTrainersCount = totalTrainersCount,
            TotalWorkoutPlansCount = totalWorkoutPlansCount,
            ActiveWorkoutAssignmentsCount = activeWorkoutAssignmentsCount,
            TotalProgressRecordsCount = totalProgressRecordsCount,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
