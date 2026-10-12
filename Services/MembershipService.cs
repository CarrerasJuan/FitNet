using FitNet.Data;
using FitNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Services;

public class MembershipService : IMembershipService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<MembershipService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<MemberAccessStatus> GetMemberStatusAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BuildDefaultInactiveStatus();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BuildDefaultInactiveStatus();
        }

        // 1. Si es Staff (Admin, Owner, Trainer), tiene acceso irrestricto
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any(r => r is "Admin" or "Owner" or "Trainer"))
        {
            var primaryRole = roles.FirstOrDefault() ?? "Staff";
            return new MemberAccessStatus
            {
                PlanName = primaryRole,
                IsActive = true,
                IsTrial = false,
                IsTrialExpired = false,
                ExpirationDate = null,
                RemainingHours = 0,
                RemainingDays = 9999,
                CanAccessPublicExercises = true,
                CanAccessFullLibrary = true,
                CanAccessWorkouts = true,
                CanAccessProgress = true,
                StatusBadge = primaryRole,
                StatusMessage = $"Acceso de gestión ({primaryRole}) activo."
            };
        }

        // 2. Consultar perfil del socio y membresía
        var member = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (member == null)
        {
            return BuildDefaultInactiveStatus();
        }

        var planName = member.Membership?.Name ?? "Free";
        var now = DateTime.UtcNow;
        var isTrial = planName.Equals("Free", StringComparison.OrdinalIgnoreCase);
        var isActive = member.TrialEndsAt > now;
        var isTrialExpired = isTrial && !isActive;

        var remainingHours = isActive
            ? Math.Max(0, (int)Math.Ceiling((member.TrialEndsAt - now).TotalHours))
            : 0;

        var remainingDays = isActive
            ? Math.Max(1, (int)Math.Ceiling((member.TrialEndsAt - now).TotalDays))
            : 0;

        // 3. Matriz de reglas de negocio
        if (isTrial)
        {
            return new MemberAccessStatus
            {
                PlanName = "Free",
                IsActive = isActive,
                IsTrial = true,
                IsTrialExpired = isTrialExpired,
                ExpirationDate = member.TrialEndsAt,
                RemainingHours = remainingHours,
                RemainingDays = 0,
                CanAccessPublicExercises = isActive,
                CanAccessFullLibrary = false,
                CanAccessWorkouts = false,
                CanAccessProgress = false,
                StatusBadge = "Free",
                StatusMessage = isActive
                    ? $"Período de prueba activo ({remainingHours} horas restantes)."
                    : "El período de prueba de 24 horas ha expirado. Mejorá tu plan para acceder al contenido."
            };
        }

        // Planes comerciales Gold o Premium
        var isGold = planName.Equals("Gold", StringComparison.OrdinalIgnoreCase);
        var isPremium = planName.Equals("Premium", StringComparison.OrdinalIgnoreCase);

        return new MemberAccessStatus
        {
            PlanName = planName,
            IsActive = isActive,
            IsTrial = false,
            IsTrialExpired = false,
            ExpirationDate = member.TrialEndsAt,
            RemainingHours = remainingHours,
            RemainingDays = remainingDays,
            CanAccessPublicExercises = isActive,
            CanAccessFullLibrary = isActive,
            CanAccessWorkouts = isActive, // Gold: generales; Premium: personalizadas
            CanAccessProgress = isActive,
            StatusBadge = planName,
            StatusMessage = isActive
                ? $"Suscripción {planName} activa ({remainingDays} días restantes)."
                : $"Tu suscripción al Plan {planName} ha vencido. Renová tu membresía para continuar entrenando."
        };
    }

    public async Task<bool> CanAccessPublicExercisesAsync(string userId)
    {
        var status = await GetMemberStatusAsync(userId);
        return status.CanAccessPublicExercises;
    }

    public async Task<bool> CanAccessFullLibraryAsync(string userId)
    {
        var status = await GetMemberStatusAsync(userId);
        return status.CanAccessFullLibrary;
    }

    public async Task<bool> CanAccessWorkoutsAsync(string userId)
    {
        var status = await GetMemberStatusAsync(userId);
        return status.CanAccessWorkouts;
    }

    public async Task<bool> CanAccessProgressAsync(string userId)
    {
        var status = await GetMemberStatusAsync(userId);
        return status.CanAccessProgress;
    }

    public async Task<bool> IsTrialExpiredAsync(string userId)
    {
        var status = await GetMemberStatusAsync(userId);
        return status.IsTrialExpired;
    }

    public async Task<bool> ChangeMembershipAsync(string userId, string planName, int durationMonths = 1)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(planName))
        {
            return false;
        }

        var member = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (member == null)
        {
            return false;
        }

        var targetPlan = await _context.Memberships
            .FirstOrDefaultAsync(m => m.Name.ToLower() == planName.Trim().ToLower());

        if (targetPlan == null)
        {
            return false;
        }

        var isFree = targetPlan.Name.Equals("Free", StringComparison.OrdinalIgnoreCase);
        var isCurrentActive = member.TrialEndsAt > DateTime.UtcNow;
        var isSamePlan = isCurrentActive && member.MembershipId == targetPlan.Id;

        member.MembershipId = targetPlan.Id;

        if (isFree)
        {
            member.TrialEndsAt = DateTime.UtcNow.AddHours(24);
        }
        else if (isSamePlan)
        {
            member.TrialEndsAt = member.TrialEndsAt.AddMonths(durationMonths);
        }
        else
        {
            member.TrialEndsAt = DateTime.UtcNow.AddMonths(durationMonths);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Membresía actualizada para usuario {UserId} a plan {PlanName}. Vencimiento: {EndsAt}",
            userId, targetPlan.Name, member.TrialEndsAt);

        return true;
    }

    private static MemberAccessStatus BuildDefaultInactiveStatus()
    {
        return new MemberAccessStatus
        {
            PlanName = "Free",
            IsActive = false,
            IsTrial = true,
            IsTrialExpired = true,
            ExpirationDate = null,
            RemainingHours = 0,
            RemainingDays = 0,
            CanAccessPublicExercises = false,
            CanAccessFullLibrary = false,
            CanAccessWorkouts = false,
            CanAccessProgress = false,
            StatusBadge = "Sin Plan",
            StatusMessage = "No se encontró un perfil activo de socio."
        };
    }
}
