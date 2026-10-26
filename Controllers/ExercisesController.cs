using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
using FitNet.ViewModels.Exercises;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

public class ExercisesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMembershipService _membershipService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipService membershipService,
        ILogger<ExercisesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _membershipService = membershipService;
        _logger = logger;
    }

    // ==========================================
    // 1. CATÁLOGO / BIBLIOTECA DE EJERCICIOS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(string? muscleGroup, string? difficulty, string? search)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var canAccessFullLibrary = false;
        var isTrialActive = false;
        var trialHoursRemaining = 0;
        var planName = "Público";

        if (isAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var status = await _membershipService.GetMemberStatusAsync(user.Id);

                // Bloqueo server-side estricto si el trial Free venció
                if (status.IsTrialExpired)
                {
                    TempData["MensajeError"] = "Tu período de prueba de 24 horas ha finalizado. Suscribite a Plan Gold o Premium para acceder al catálogo de ejercicios.";
                    return RedirectToAction("Checkout", "Billing", new { plan = "gold" });
                }

                canAccessFullLibrary = status.CanAccessFullLibrary;
                isTrialActive = status.IsTrial && status.IsActive;
                trialHoursRemaining = status.RemainingHours;
                planName = status.PlanName;

                // Datos para topbar si usa LayoutDashboard
                ViewData["PlanName"] = status.PlanName;
                ViewData["IsTrialActive"] = isTrialActive;
                ViewData["TrialHoursRemaining"] = trialHoursRemaining;
                ViewData["DaysRemaining"] = status.RemainingDays;
                ViewData["ExpirationDate"] = status.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";
            }
        }

        // Consulta base con filtro de seguridad server-side
        var query = _context.Exercises
            .AsNoTracking()
            .Where(e => e.IsActive);

        // Si NO tiene acceso a la biblioteca completa, solo ve ejercicios públicos
        if (!canAccessFullLibrary)
        {
            query = query.Where(e => e.IsPublic);
        }

        // Filtros opcionales
        if (!string.IsNullOrWhiteSpace(muscleGroup))
        {
            query = query.Where(e => e.MuscleGroup == muscleGroup);
        }

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            query = query.Where(e => e.Difficulty == difficulty);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchClean = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(searchClean) || 
                                     (e.Description != null && e.Description.ToLower().Contains(searchClean)));
        }

        var exercises = await query
            .OrderBy(e => e.MuscleGroup)
            .ThenBy(e => e.Name)
            .ToListAsync();

        // Conteo general para métricas del dashboard
        var totalCount = await _context.Exercises.CountAsync(e => e.IsActive);
        var publicCount = await _context.Exercises.CountAsync(e => e.IsActive && e.IsPublic);
        var exclusiveCount = totalCount - publicCount;

        var viewModel = new ExerciseCatalogViewModel
        {
            Exercises = exercises,
            SelectedMuscleGroup = muscleGroup,
            SelectedDifficulty = difficulty,
            SearchQuery = search,
            CanAccessFullLibrary = canAccessFullLibrary,
            IsTrialActive = isTrialActive,
            TrialHoursRemaining = trialHoursRemaining,
            PlanName = planName,
            TotalExercisesCount = totalCount,
            PublicExercisesCount = publicCount,
            ExclusiveExercisesCount = exclusiveCount
        };

        return View(viewModel);
    }

    // ==========================================
    // 2. DETALLE DE UN EJERCICIO
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (exercise == null)
        {
            return NotFound();
        }

        // Validación de bloqueo server-side para trial vencido en rutas manuales directas
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var status = await _membershipService.GetMemberStatusAsync(currentUser.Id);
                if (status.IsTrialExpired)
                {
                    TempData["MensajeError"] = "Tu período de prueba de 24 horas ha finalizado. Suscribite a Plan Gold o Premium para acceder al detalle de ejercicios.";
                    return RedirectToAction("Checkout", "Billing", new { plan = "gold" });
                }
            }
        }

        // Validación de permisos server-side para ejercicios exclusivos
        if (!exercise.IsPublic)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["MensajeError"] = "El ejercicio seleccionado es exclusivo de los planes Gold y Premium. Iniciá sesión con tu cuenta.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Exercises/Details/{id}" });
            }

            var canAccess = await _membershipService.CanAccessFullLibraryAsync(user.Id);
            if (!canAccess)
            {
                TempData["MensajeUpgrade"] = $"El ejercicio '{exercise.Name}' requiere suscripción activa a Plan Gold o Premium.";
                return RedirectToAction("Index");
            }
        }

        return View(exercise);
    }
}
