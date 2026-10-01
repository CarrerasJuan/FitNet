using FitNet.Data;
using FitNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<BillingController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 1. PANTALLA DE CHECKOUT SIMULADO
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Checkout(string plan = "gold", string periodo = "mensual")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var normalizedPlan = plan.Trim().ToLowerInvariant();
        var targetPlan = await _context.Memberships
            .FirstOrDefaultAsync(m => m.Name.ToLower() == normalizedPlan);

        if (targetPlan == null || targetPlan.Name == "Free")
        {
            targetPlan = await _context.Memberships.FirstAsync(m => m.Name == "Gold");
        }

        var isSemestral = periodo.Trim().ToLowerInvariant() == "semestral";
        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var currentPlanName = memberProfile?.Membership?.Name ?? "Free";
        var isCurrentPlanActive = memberProfile != null && memberProfile.TrialEndsAt > DateTime.UtcNow;
        var isAlreadySamePlan = isCurrentPlanActive && currentPlanName.Equals(targetPlan.Name, StringComparison.OrdinalIgnoreCase);

        var daysRemaining = isCurrentPlanActive && currentPlanName != "Free" && memberProfile != null
            ? Math.Max(1, (int)Math.Ceiling((memberProfile.TrialEndsAt - DateTime.UtcNow).TotalDays))
            : 0;

        ViewBag.Periodo = isSemestral ? "semestral" : "mensual";
        ViewBag.TargetPlan = targetPlan;
        ViewBag.User = user;
        ViewBag.AlreadySamePlan = isAlreadySamePlan;
        ViewBag.CurrentPlanName = currentPlanName;
        ViewBag.DaysRemaining = daysRemaining;
        ViewBag.ExpirationDate = memberProfile?.TrialEndsAt.ToString("dd/MM/yyyy") ?? "";

        // Datos para el Navbar Topbar
        ViewData["PlanName"] = currentPlanName;
        ViewData["IsTrialActive"] = isCurrentPlanActive;
        ViewData["DaysRemaining"] = daysRemaining;
        ViewData["ExpirationDate"] = memberProfile?.TrialEndsAt.ToString("dd/MM/yyyy") ?? "";

        return View();
    }

    // ==========================================
    // 2. PROCESAMIENTO SEGURO DEL UPGRADE / EXTENSIÓN
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessUpgrade(string planCode, string periodo = "mensual")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(planCode))
        {
            TempData["MensajeError"] = "Plan seleccionado no válido.";
            return RedirectToAction("Dashboard", "Members");
        }

        var normalizedPlan = planCode.Trim().ToLowerInvariant();
        var targetPlan = await _context.Memberships
            .FirstOrDefaultAsync(m => m.Name.ToLower() == normalizedPlan);

        if (targetPlan == null || targetPlan.Name == "Free")
        {
            TempData["MensajeError"] = "El plan solicitado no admite suscripción directa.";
            return RedirectToAction("Dashboard", "Members");
        }

        var isSemestral = periodo.Trim().ToLowerInvariant() == "semestral";
        int monthsToAdd = isSemestral ? 6 : 1;

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (memberProfile == null)
        {
            memberProfile = new MemberProfile
            {
                UserId = user.Id,
                MembershipId = targetPlan.Id,
                TrialEndsAt = DateTime.UtcNow.AddMonths(monthsToAdd),
                CreatedAt = DateTime.UtcNow
            };
            _context.MemberProfiles.Add(memberProfile);
        }
        else
        {
            var isCurrentActive = memberProfile.TrialEndsAt > DateTime.UtcNow;
            var isSamePlan = isCurrentActive && memberProfile.MembershipId == targetPlan.Id;

            if (isSamePlan)
            {
                // Si el socio ya tiene este plan activo, la fecha de vencimiento se EXTIENDE sin perder días previos
                memberProfile.TrialEndsAt = memberProfile.TrialEndsAt.AddMonths(monthsToAdd);
            }
            else
            {
                // Si viene de Free (o upgrade de Gold a Premium), se actualiza el plan y el período corre a partir de hoy
                memberProfile.MembershipId = targetPlan.Id;
                memberProfile.TrialEndsAt = DateTime.UtcNow.AddMonths(monthsToAdd);
            }
        }

        await _context.SaveChangesAsync();

        var periodoTexto = isSemestral ? "Semestral (6 meses con 20% OFF)" : "Mensual";
        _logger.LogInformation("Usuario {Email} procesó suscripción al plan {PlanName} ({Periodo}). Nuevo vencimiento: {Vencimiento}", 
            user.Email, targetPlan.Name, periodoTexto, memberProfile.TrialEndsAt);

        TempData["MensajeExito"] = $"¡Excelente! Tu suscripción al Plan {targetPlan.Name} ({periodoTexto}) fue procesada con éxito. Nuevo vencimiento: {memberProfile.TrialEndsAt:dd/MM/yyyy}.";
        return RedirectToAction("Dashboard", "Members");
    }
}
