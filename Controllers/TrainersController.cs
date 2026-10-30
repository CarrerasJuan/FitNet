using FitNet.Data;
using FitNet.Models;
using FitNet.ViewModels.Trainers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize(Roles = "Trainer,Admin,Owner")]
public class TrainersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TrainersController> _logger;

    public TrainersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TrainersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 1. DASHBOARD DEL ENTRENADOR
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var trainerProfile = await _context.TrainerProfiles
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        var isStaffAdmin = User.IsInRole("Admin") || User.IsInRole("Owner");

        // Socios asignados activamente
        var assignedAssignments = await _context.WorkoutAssignments
            .Include(a => a.Member)
                .ThenInclude(m => m.User)
            .Include(a => a.WorkoutPlan)
            .Where(a => (a.AssignedByTrainerId == user.Id || isStaffAdmin) && a.IsActive)
            .ToListAsync();

        ViewBag.AssignedCount = assignedAssignments.Count;
        ViewBag.Assignments = assignedAssignments;
        ViewBag.TrainerName = $"{user.FirstName} {user.LastName}";
        ViewBag.Specialty = trainerProfile?.Specialty ?? "Preparación Física";

        return View();
    }

    // ==========================================
    // 2. PANEL DE ALUMNOS ASIGNADOS (Paginado Server-Side)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Members(string? search = null, int page = 1, int pageSize = 8)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var trainerProfile = await _context.TrainerProfiles
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        var trainerName = $"{user.FirstName} {user.LastName}";
        var specialty = trainerProfile?.Specialty ?? "Preparación Física";

        var isStaffAdmin = User.IsInRole("Admin") || User.IsInRole("Owner");

        // Base query: todos los socios activos con membresía del club (Gold o Premium)
        var baseQuery = _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .Include(m => m.Assignments.Where(a => a.IsActive && (!a.EndDate.HasValue || a.EndDate.Value >= DateTime.UtcNow.Date)))
                .ThenInclude(a => a.WorkoutPlan)
                    .ThenInclude(wp => wp.WorkoutExercises)
            .Include(m => m.ProgressRecords)
            .Where(m => m.User.IsActive && m.Membership.Name != "Free")
            .AsNoTracking();

        // Filtro de búsqueda server-side
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            baseQuery = baseQuery.Where(m =>
                m.User.FirstName.ToLower().Contains(term) ||
                m.User.LastName.ToLower().Contains(term) ||
                (m.User.Email != null && m.User.Email.ToLower().Contains(term)));
        }

        // Paginado directo en MySQL (Skip / Take)
        var totalItems = await baseQuery.CountAsync();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var members = await baseQuery
            .OrderBy(m => m.User.LastName)
            .ThenBy(m => m.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var studentItems = members.Select(m =>
        {
            var activeAssignment = m.Assignments.FirstOrDefault(a => a.IsActive);
            var latestProgress = m.ProgressRecords.OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt).FirstOrDefault();

            int? age = null;
            if (m.BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var a = today.Year - m.BirthDate.Value.Year;
                if (m.BirthDate.Value.Date > today.AddYears(-a)) a--;
                age = a;
            }

            return new TrainerStudentItemViewModel
            {
                MemberProfileId = m.Id,
                UserId = m.UserId,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email ?? string.Empty,
                PhoneNumber = m.User.PhoneNumber,
                AvatarPath = m.User.AvatarPath,
                PlanName = m.Membership?.Name ?? "Premium",
                Height = m.Height,
                Age = age,
                ActiveWorkoutPlanId = activeAssignment?.WorkoutPlanId,
                ActiveWorkoutPlanName = activeAssignment?.WorkoutPlan?.Name,
                AssignmentStartDate = activeAssignment?.StartDate,
                ExercisesInRoutineCount = activeAssignment?.WorkoutPlan?.WorkoutExercises.Count ?? 0,
                LatestWeight = latestProgress?.Weight,
                LatestBodyFat = latestProgress?.BodyFat,
                LatestProgressDate = latestProgress?.Date,
                TotalProgressRecords = m.ProgressRecords.Count
            };
        }).ToList();

        var totalAssigned = await _context.MemberProfiles
            .CountAsync(m => m.User.IsActive && m.Membership.Name != "Free");

        var activeRoutines = await _context.WorkoutAssignments
            .CountAsync(a => a.IsActive && (!a.EndDate.HasValue || a.EndDate.Value >= DateTime.UtcNow.Date));

        var withoutRoutine = Math.Max(0, totalAssigned - activeRoutines);

        var viewModel = new TrainerMembersViewModel
        {
            Students = studentItems,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TrainerName = trainerName,
            Specialty = specialty,
            TotalAssignedCount = totalAssigned,
            ActiveRoutinesCount = activeRoutines,
            WithoutRoutineCount = withoutRoutine
        };

        return View(viewModel);
    }

    // ==========================================
    // 3. SEGUIMIENTO DE PROGRESO DE UN ALUMNO
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> MemberProgress(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .Include(m => m.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.WorkoutPlan)
            .Include(m => m.ProgressRecords)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (memberProfile == null)
        {
            TempData["MensajeError"] = "No se encontró el alumno solicitado.";
            return RedirectToAction("Members");
        }

        var isStaffOrTrainer = User.IsInRole("Admin") || User.IsInRole("Owner") || User.IsInRole("Trainer");

        if (!isStaffOrTrainer || memberProfile.Membership.Name == "Free")
        {
            TempData["MensajeError"] = "No tienes permisos para visualizar el progreso de este socio.";
            return RedirectToAction("Members");
        }

        var records = memberProfile.ProgressRecords
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();

        var latestWeight = records.FirstOrDefault(r => r.Weight.HasValue)?.Weight;
        var latestBodyFat = records.FirstOrDefault(r => r.BodyFat.HasValue)?.BodyFat;
        var initialWeight = records.LastOrDefault(r => r.Weight.HasValue)?.Weight;
        var weightDiff = (latestWeight.HasValue && initialWeight.HasValue)
            ? (decimal?)(latestWeight.Value - initialWeight.Value)
            : null;

        int? age = null;
        if (memberProfile.BirthDate.HasValue)
        {
            var today = DateTime.Today;
            var a = today.Year - memberProfile.BirthDate.Value.Year;
            if (memberProfile.BirthDate.Value.Date > today.AddYears(-a)) a--;
            age = a;
        }

        var activeAssignment = memberProfile.Assignments.FirstOrDefault(a => a.IsActive);

        var viewModel = new TrainerStudentProgressViewModel
        {
            MemberProfileId = memberProfile.Id,
            StudentFullName = $"{memberProfile.User.FirstName} {memberProfile.User.LastName}",
            StudentEmail = memberProfile.User.Email ?? string.Empty,
            StudentAvatarPath = memberProfile.User.AvatarPath,
            PlanName = memberProfile.Membership?.Name ?? "Premium",
            Height = memberProfile.Height,
            Age = age,
            ActiveWorkoutPlanId = activeAssignment?.WorkoutPlanId,
            ActiveWorkoutPlanName = activeAssignment?.WorkoutPlan?.Name,
            Records = records,
            LatestWeight = latestWeight,
            LatestBodyFat = latestBodyFat,
            InitialWeight = initialWeight,
            WeightDifference = weightDiff
        };

        return View(viewModel);
    }

    // ==========================================
    // 4. GESTIÓN DE STOCK DE EJERCICIOS (SPA VUE 3)
    // ==========================================
    [HttpGet]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public IActionResult Exercises()
    {
        return View("~/Views/Admin/Exercises.cshtml");
    }
}
