using System.Security.Claims;
using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
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
    private readonly IMembershipService _membershipService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipService membershipService,
        IFileStorageService fileStorageService,
        ILogger<MembersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _membershipService = membershipService;
        _fileStorageService = fileStorageService;
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

        var status = await _membershipService.GetMemberStatusAsync(user.Id);

        var activeAssignment = memberProfile?.Assignments.FirstOrDefault();
        var latestProgress = memberProfile?.ProgressRecords.FirstOrDefault();

        // Datos para el Topbar y Sidebar
        ViewData["PlanName"] = status.PlanName;
        ViewData["IsTrialActive"] = status.IsTrial && status.IsActive;
        ViewData["TrialHoursRemaining"] = status.RemainingHours;
        ViewData["DaysRemaining"] = status.RemainingDays;
        ViewData["ExpirationDate"] = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";

        var model = new MemberDashboardViewModel
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            AvatarPath = user.AvatarPath,
            PlanName = status.PlanName,
            IsTrialActive = status.IsTrial && status.IsActive,
            TrialEndsAt = status.ExpirationDate ?? DateTime.UtcNow,
            TrialHoursRemaining = status.RemainingHours,
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

        var status = await _membershipService.GetMemberStatusAsync(user.Id);

        if (!status.CanAccessWorkouts)
        {
            TempData["MensajeUpgrade"] = "Las rutinas guiadas del gimnasio requieren suscripción a Plan Gold o Premium.";
            return RedirectToAction("Dashboard");
        }

        // Datos para el Topbar y Sidebar
        ViewData["PlanName"] = status.PlanName;
        ViewData["IsTrialActive"] = status.IsTrial && status.IsActive;
        ViewData["TrialHoursRemaining"] = status.RemainingHours;
        ViewData["DaysRemaining"] = status.RemainingDays;
        ViewData["ExpirationDate"] = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";

        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var activeAssignments = new List<WorkoutAssignment>();
        if (memberProfile != null)
        {
            activeAssignments = await _context.WorkoutAssignments
                .Include(a => a.AssignedByTrainer)
                .Include(a => a.WorkoutPlan)
                    .ThenInclude(wp => wp.WorkoutExercises.OrderBy(we => we.Order))
                        .ThenInclude(we => we.Exercise)
                .Where(a => a.MemberId == memberProfile.Id && a.IsActive)
                .ToListAsync();
        }

        var generalPlans = await _context.WorkoutPlans
            .Include(wp => wp.Trainer)
            .Include(wp => wp.WorkoutExercises.OrderBy(we => we.Order))
                .ThenInclude(we => we.Exercise)
            .Where(wp => wp.IsGeneral)
            .ToListAsync();

        var viewModel = new MemberWorkoutsViewModel
        {
            ActiveAssignments = activeAssignments,
            GeneralPlans = generalPlans,
            PlanName = status.PlanName,
            RemainingDays = status.RemainingDays,
            ExpirationDate = status.ExpirationDate
        };

        return View(viewModel);
    }

    // ==========================================
    // 3. MI PROGRESO (Requiere Gold / Premium)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Progress()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var status = await _membershipService.GetMemberStatusAsync(user.Id);
        if (!status.CanAccessProgress)
        {
            TempData["MensajeUpgrade"] = "El registro continuo de peso y fotos de progreso requiere Plan Gold o Premium.";
            return RedirectToAction("Dashboard");
        }

        // Datos para el Topbar y Sidebar
        ViewData["PlanName"] = status.PlanName;
        ViewData["IsTrialActive"] = status.IsTrial && status.IsActive;
        ViewData["TrialHoursRemaining"] = status.RemainingHours;
        ViewData["DaysRemaining"] = status.RemainingDays;
        ViewData["ExpirationDate"] = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";

        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var records = new List<ProgressRecord>();
        if (memberProfile != null)
        {
            records = await _context.ProgressRecords
                .Where(p => p.MemberId == memberProfile.Id)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        var latestWeight = records.FirstOrDefault(r => r.Weight.HasValue)?.Weight;
        var latestBodyFat = records.FirstOrDefault(r => r.BodyFat.HasValue)?.BodyFat;
        var initialWeight = records.LastOrDefault(r => r.Weight.HasValue)?.Weight;
        var weightDiff = (latestWeight.HasValue && initialWeight.HasValue) 
            ? (decimal?)(latestWeight.Value - initialWeight.Value) 
            : null;

        var viewModel = new MemberProgressViewModel
        {
            Records = records,
            LatestWeight = latestWeight,
            LatestBodyFat = latestBodyFat,
            InitialWeight = initialWeight,
            WeightDifference = weightDiff,
            PlanName = status.PlanName
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProgress(MemberProgressViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var status = await _membershipService.GetMemberStatusAsync(user.Id);
        if (!status.CanAccessProgress)
        {
            TempData["MensajeUpgrade"] = "El registro de progreso requiere Plan Gold o Premium.";
            return RedirectToAction("Dashboard");
        }

        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (memberProfile == null)
        {
            TempData["MensajeError"] = "No se encontró el perfil de socio.";
            return RedirectToAction("Dashboard");
        }

        // Validar foto de progreso si se adjuntó
        if (model.ProgressPhoto != null)
        {
            if (!_fileStorageService.ValidateImage(model.ProgressPhoto, out var photoError))
            {
                ModelState.AddModelError("ProgressPhoto", photoError ?? "Fotografía no válida.");
            }
        }

        // Asegurar que al menos se envíe peso, grasa, nota o foto
        if (!model.Weight.HasValue && !model.BodyFat.HasValue && string.IsNullOrWhiteSpace(model.Notes) && model.ProgressPhoto == null)
        {
            ModelState.AddModelError("", "Ingresá al menos el peso corporal, porcentaje de grasa, una nota o foto de evolución.");
        }

        if (!ModelState.IsValid)
        {
            // Recargar métricas e historial
            var records = await _context.ProgressRecords
                .Where(p => p.MemberId == memberProfile.Id)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            model.Records = records;
            model.LatestWeight = records.FirstOrDefault(r => r.Weight.HasValue)?.Weight;
            model.LatestBodyFat = records.FirstOrDefault(r => r.BodyFat.HasValue)?.BodyFat;
            model.InitialWeight = records.LastOrDefault(r => r.Weight.HasValue)?.Weight;
            model.WeightDifference = (model.LatestWeight.HasValue && model.InitialWeight.HasValue)
                ? (decimal?)(model.LatestWeight.Value - model.InitialWeight.Value)
                : null;
            model.PlanName = status.PlanName;

            ViewData["PlanName"] = status.PlanName;
            ViewData["IsTrialActive"] = status.IsTrial && status.IsActive;
            ViewData["DaysRemaining"] = status.RemainingDays;
            ViewData["ExpirationDate"] = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";

            return View("Progress", model);
        }

        string? savedImagePath = null;
        if (model.ProgressPhoto != null)
        {
            try
            {
                savedImagePath = await _fileStorageService.SaveFileAsync(model.ProgressPhoto, "progress");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar foto de progreso para el usuario {UserId}", user.Id);
                TempData["MensajeError"] = "Ocurrió un error al guardar la foto de progreso.";
                return RedirectToAction("Progress");
            }
        }

        var newRecord = new ProgressRecord
        {
            MemberId = memberProfile.Id,
            Date = model.Date,
            Weight = model.Weight,
            BodyFat = model.BodyFat,
            Notes = model.Notes?.Trim(),
            ImagePath = savedImagePath,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProgressRecords.Add(newRecord);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Registro de progreso guardado con éxito.";
        return RedirectToAction("Progress");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProgress(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (memberProfile == null) return RedirectToAction("Dashboard");

        var record = await _context.ProgressRecords
            .FirstOrDefaultAsync(p => p.Id == id && p.MemberId == memberProfile.Id);

        if (record != null)
        {
            if (!string.IsNullOrEmpty(record.ImagePath))
            {
                await _fileStorageService.DeleteFileAsync(record.ImagePath);
            }

            _context.ProgressRecords.Remove(record);
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = "El registro de progreso fue eliminado.";
        }

        return RedirectToAction("Progress");
    }
}
