using System.Security.Claims;
using FitNet.Data;
using FitNet.Models;
using FitNet.ViewModels.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<MembersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 1. MINI DASHBOARD DEL SOCIO (ATLETA)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .Include(m => m.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.WorkoutPlan)
                    .ThenInclude(wp => wp.WorkoutExercises)
            .Include(m => m.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.AssignedByTrainer)
            .Include(m => m.ProgressRecords.OrderByDescending(p => p.Date))
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        // Si es un usuario administrativo/entrenador probando la vista sin MemberProfile previo
        var planName = memberProfile?.Membership?.Name ?? "Free";
        var trialEndsAt = memberProfile?.TrialEndsAt ?? DateTime.UtcNow.AddHours(24);
        var isTrialActive = trialEndsAt > DateTime.UtcNow;
        var trialHours = isTrialActive ? Math.Max(0, (int)Math.Ceiling((trialEndsAt - DateTime.UtcNow).TotalHours)) : 0;

        var activeAssignment = memberProfile?.Assignments.FirstOrDefault();
        var latestProgress = memberProfile?.ProgressRecords.FirstOrDefault();

        var daysRemaining = isTrialActive && planName != "Free"
            ? Math.Max(1, (int)Math.Ceiling((trialEndsAt - DateTime.UtcNow).TotalDays))
            : 0;

        // Datos para el Topbar y Sidebar
        ViewData["PlanName"] = planName;
        ViewData["IsTrialActive"] = isTrialActive;
        ViewData["TrialHoursRemaining"] = trialHours;
        ViewData["DaysRemaining"] = daysRemaining;
        ViewData["ExpirationDate"] = trialEndsAt.ToString("dd/MM/yyyy");

        var model = new MemberDashboardViewModel
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            AvatarPath = user.AvatarPath,
            PlanName = planName,
            IsTrialActive = isTrialActive,
            TrialEndsAt = trialEndsAt,
            TrialHoursRemaining = trialHours,
            Height = memberProfile?.Height,
            CurrentWeight = latestProgress?.Weight,
            ActiveWorkoutTitle = activeAssignment?.WorkoutPlan?.Name,
            ActiveWorkoutExercisesCount = activeAssignment?.WorkoutPlan?.WorkoutExercises.Count ?? 0,
            AssignedTrainerName = activeAssignment?.AssignedByTrainer != null 
                ? $"{activeAssignment.AssignedByTrainer.FirstName} {activeAssignment.AssignedByTrainer.LastName}" 
                : null,
            AssignedTrainerSpecialty = "Fuerza & Acondicionamiento",
            AssignedTrainerAvatar = activeAssignment?.AssignedByTrainer?.AvatarPath
        };

        return View(model);
    }

    // ==========================================
    // 2. MIS RUTINAS (Requiere Gold / Premium)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Workouts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        // Si es Free, informar que requiere Gold
        if (memberProfile?.Membership?.Name == "Free")
        {
            TempData["MensajeUpgrade"] = "Las rutinas guiadas del gimnasio requieren suscripción a Plan Gold o Premium.";
            return RedirectToAction("Dashboard");
        }

        return View();
    }

    // ==========================================
    // 3. MI PROGRESO (Requiere Gold / Premium)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Progress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (memberProfile?.Membership?.Name == "Free")
        {
            TempData["MensajeUpgrade"] = "El registro continuo de peso y fotos de progreso requiere Plan Gold.";
            return RedirectToAction("Dashboard");
        }

        return View();
    }
}
