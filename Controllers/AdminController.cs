using System.Text;
using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
using FitNet.ViewModels.Admin;
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
            .OrderBy(t => t.Id)
            .ToListAsync();

        var trainerUserIds = trainers.Select(t => t.UserId).ToList();

        // Conteo de planes creados por cada entrenador
        var plansCountByTrainer = await _context.WorkoutPlans
            .Where(wp => wp.TrainerId != null && trainerUserIds.Contains(wp.TrainerId))
            .GroupBy(wp => wp.TrainerId!)
            .Select(g => new { TrainerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.TrainerId, g => g.Count);

        // Conteo de atletas con asignaciones activas por cada entrenador
        var activeAthletesByTrainer = await _context.WorkoutAssignments
            .Where(wa => wa.AssignedByTrainerId != null && trainerUserIds.Contains(wa.AssignedByTrainerId) && wa.IsActive)
            .GroupBy(wa => wa.AssignedByTrainerId!)
            .Select(g => new { TrainerId = g.Key, Count = g.Select(x => x.MemberId).Distinct().Count() })
            .ToDictionaryAsync(g => g.TrainerId, g => g.Count);

        var viewModel = new AdminTrainersViewModel
        {
            TotalTrainers = trainers.Count,
            TotalWorkoutsCreated = await _context.WorkoutPlans.CountAsync(wp => wp.TrainerId != null),
            TotalActiveAthletes = await _context.WorkoutAssignments
                .Where(wa => wa.IsActive && wa.AssignedByTrainerId != null)
                .Select(wa => wa.MemberId)
                .Distinct()
                .CountAsync(),
            Trainers = trainers.Select(t => new TrainerStaffItemViewModel
            {
                Id = t.Id,
                UserId = t.UserId,
                FullName = t.User?.FullName ?? "Entrenador",
                Email = t.User?.Email ?? "Sin email",
                PhoneNumber = t.User?.PhoneNumber,
                AvatarPath = t.User?.AvatarPath,
                Specialty = !string.IsNullOrWhiteSpace(t.Specialty) ? t.Specialty : "Acondicionamiento Físico General",
                Biography = !string.IsNullOrWhiteSpace(t.Biography) ? t.Biography : "Entrenador profesional del staff de FitNet.",
                CreatedAt = t.CreatedAt,
                IsActive = t.User?.IsActive ?? true,
                TotalPlansCount = plansCountByTrainer.GetValueOrDefault(t.UserId, 0),
                ActiveAthletesCount = activeAthletesByTrainer.GetValueOrDefault(t.UserId, 0),
                Shift = t.Specialty?.Contains("Cross", StringComparison.OrdinalIgnoreCase) == true
                    ? "Turno Mañana"
                    : (t.Specialty?.Contains("Rehab", StringComparison.OrdinalIgnoreCase) == true || t.Specialty?.Contains("Funcional", StringComparison.OrdinalIgnoreCase) == true
                        ? "Turno Tarde"
                        : "Turno Completo")
            }).ToList()
        };

        return View(viewModel);
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

    // ==========================================
    // 6. EXPORTACIÓN DE PADRÓN DE SOCIOS EN CSV
    // Exclusivo para el rol Owner con codificación UTF-8 con BOM (Compatible con Microsoft Excel)
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> ExportMembersCsv()
    {
        var members = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .OrderBy(m => m.Id)
            .ToListAsync();

        var builder = new StringBuilder();

        // Cabecera formal en español delimitada por punto y coma (estándar regional de Excel)
        builder.AppendLine("ID Socio;Nombre Completo;Correo Electrónico;Teléfono;Plan Actual;Precio Mensual;Estado de Cuenta;Fecha de Registro;Vencimiento Membresía;Trial Vencido");

        foreach (var m in members)
        {
            var isFree = m.Membership?.Name == "Free";
            var isTrialExpired = isFree && DateTime.UtcNow > m.TrialEndsAt;
            var planStatus = m.User?.IsActive == true ? "Activo" : "Inactivo";
            var expiration = m.TrialEndsAt.ToString("yyyy-MM-dd HH:mm");
            var registeredAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            var price = m.Membership?.MonthlyPrice ?? 0m;

            builder.AppendLine(string.Join(";",
                EscapeCsv(m.Id.ToString()),
                EscapeCsv(m.User?.FullName ?? "—"),
                EscapeCsv(m.User?.Email ?? "—"),
                EscapeCsv(m.User?.PhoneNumber ?? "—"),
                EscapeCsv(m.Membership?.Name ?? "Free"),
                EscapeCsv($"${price:N2}"),
                EscapeCsv(planStatus),
                EscapeCsv(registeredAt),
                EscapeCsv(expiration),
                EscapeCsv(isTrialExpired ? "Sí" : "No")
            ));
        }

        // Emisión de bytes con BOM (Byte Order Mark) para forzar detección UTF-8 en Excel
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = encoding.GetBytes(builder.ToString());
        var fileName = $"FitNet_Socios_{DateTime.Now:yyyyMMdd_HHmm}.csv";

        _logger.LogInformation("Padrón de {Count} socios exportado exitosamente en formato CSV por el Owner '{User}'.", 
            members.Count, User.Identity?.Name);

        return File(bytes, "text/csv; charset=utf-8", fileName);

        static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
