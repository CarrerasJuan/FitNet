using FitNet.Data;
using FitNet.Models;
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

        // Socios asignados activamente
        var assignedAssignments = await _context.WorkoutAssignments
            .Include(a => a.Member)
                .ThenInclude(m => m.User)
            .Include(a => a.WorkoutPlan)
            .Where(a => a.AssignedByTrainerId == user.Id && a.IsActive)
            .ToListAsync();

        ViewBag.AssignedCount = assignedAssignments.Count;
        ViewBag.Assignments = assignedAssignments;
        ViewBag.TrainerName = $"{user.FirstName} {user.LastName}";
        ViewBag.Specialty = trainerProfile?.Specialty ?? "Preparación Física";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Members()
    {
        return await Dashboard();
    }
}
