using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize(Roles = "Member")]
public class BillingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMembershipService _membershipService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipService membershipService,
        ILogger<BillingController> logger)
    {
        _context = context;
        _userManager = userManager;
        _membershipService = membershipService;
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
        var status = await _membershipService.GetMemberStatusAsync(user.Id);

        var currentPlanName = status.PlanName;
        var isCurrentPlanActive = status.IsActive;
        var isAlreadySamePlan = isCurrentPlanActive && currentPlanName.Equals(targetPlan.Name, StringComparison.OrdinalIgnoreCase);
        var daysRemaining = status.RemainingDays;
        var expirationDate = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";

        ViewBag.Periodo = isSemestral ? "semestral" : "mensual";
        ViewBag.TargetPlan = targetPlan;
        ViewBag.User = user;
        ViewBag.AlreadySamePlan = isAlreadySamePlan;
        ViewBag.CurrentPlanName = currentPlanName;
        ViewBag.DaysRemaining = daysRemaining;
        ViewBag.ExpirationDate = expirationDate;

        // Datos para el Navbar Topbar
        ViewData["PlanName"] = currentPlanName;
        ViewData["IsTrialActive"] = status.IsTrial && status.IsActive;
        ViewData["DaysRemaining"] = daysRemaining;
        ViewData["ExpirationDate"] = expirationDate;

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

        var success = await _membershipService.ChangeMembershipAsync(user.Id, targetPlan.Name, monthsToAdd);
        if (!success)
        {
            TempData["MensajeError"] = "No se pudo procesar la suscripción. Intente nuevamente.";
            return RedirectToAction("Dashboard", "Members");
        }

        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var periodoTexto = isSemestral ? "Semestral (6 meses con 20% OFF)" : "Mensual";
        _logger.LogInformation("Usuario {Email} procesó suscripción al plan {PlanName} ({Periodo}). Nuevo vencimiento: {Vencimiento}", 
            user.Email, targetPlan.Name, periodoTexto, memberProfile?.TrialEndsAt);

        TempData["MensajeExito"] = $"¡Excelente! Tu suscripción al Plan {targetPlan.Name} ({periodoTexto}) fue procesada con éxito. Nuevo vencimiento: {memberProfile?.TrialEndsAt:dd/MM/yyyy}.";
        return RedirectToAction("Dashboard", "Members");
    }
}
