using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize(Roles = "Admin,Owner,Trainer")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMembershipService _membershipService;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipService membershipService,
        IMetricsService metricsService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _membershipService = membershipService;
        _metricsService = metricsService;
        _logger = logger;
    }

    // ==========================================
    // 0. ACCESO RÁPIDO / ALIAS (/ad y /admin)
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Admin,Owner")]
    [Route("ad")]
    [Route("admin")]
    public IActionResult AdminAlias()
    {
        return RedirectToAction("Dashboard");
    }

    // ==========================================
    // 1. DASHBOARD DE ADMINISTRACIÓN GENERAL
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Dashboard()
    {
        var totalMembers = await _context.MemberProfiles.CountAsync();
        var totalTrainers = await _context.TrainerProfiles.CountAsync();
        var goldMembers = await _context.MemberProfiles.CountAsync(m => m.Membership.Name == "Gold");
        var premiumMembers = await _context.MemberProfiles.CountAsync(m => m.Membership.Name == "Premium");
        var freeMembers = await _context.MemberProfiles.CountAsync(m => m.Membership.Name == "Free");

        var recentMembers = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalMembers = totalMembers;
        ViewBag.TotalTrainers = totalTrainers;
        ViewBag.GoldMembers = goldMembers;
        ViewBag.PremiumMembers = premiumMembers;
        ViewBag.FreeMembers = freeMembers;
        ViewBag.RecentMembers = recentMembers;

        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Members()
    {
        var members = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        ViewBag.AvailablePlans = await _context.Memberships.Where(m => m.IsActive).ToListAsync();
        return View(members);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Owner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMemberPlan(string userId, string planName, int durationMonths = 1, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(planName))
        {
            TempData["MensajeError"] = "Parámetros de socio o plan no válidos.";
            return RedirectToAction("Members");
        }

        var success = await _membershipService.ChangeMembershipAsync(userId, planName, durationMonths);
        if (success)
        {
            TempData["MensajeExito"] = $"Membresía del socio actualizada a '{planName}' con éxito ({durationMonths} mes/es).";
        }
        else
        {
            TempData["MensajeError"] = "No se pudo actualizar la membresía del socio.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Members");
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Trainers()
    {
        var trainers = await _context.TrainerProfiles
            .Include(t => t.User)
            .ToListAsync();

        return View(trainers);
    }

    // ==========================================
    // 4. GESTIÓN REACTIVA DE EJERCICIOS (VUE 3 + AJAX)
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Admin,Owner,Trainer")]
    public IActionResult Exercises()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Metrics()
    {
        var model = await _metricsService.GetBusinessMetricsAsync();
        return View(model);
    }

    // ==========================================
    // 5. GESTIÓN DE RUTINAS GENERALES (REDIRECCIÓN)
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Admin,Owner")]
    public IActionResult WorkoutPlans()
    {
        return RedirectToAction("Index", "WorkoutPlans", new { filter = "general" });
    }
}
