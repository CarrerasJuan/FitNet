using FitNet.Data;
using FitNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize(Roles = "Admin,Owner")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 0. ACCESO RÁPIDO / ALIAS (/ad y /admin)
    // ==========================================
    [HttpGet]
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
    public async Task<IActionResult> Members()
    {
        var members = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return View(members);
    }

    [HttpGet]
    public async Task<IActionResult> Trainers()
    {
        var trainers = await _context.TrainerProfiles
            .Include(t => t.User)
            .ToListAsync();

        return View(trainers);
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    public IActionResult Metrics()
    {
        return View();
    }
}
