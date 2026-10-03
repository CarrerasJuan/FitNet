using System.Security.Claims;
using FitNet.Data;
using FitNet.Models;
using FitNet.Services;
using FitNet.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    // ==========================================
    // 1. INICIO DE SESIÓN (LOGIN)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                return await RedirectAfterLoginAsync(currentUser, returnUrl);
            }
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Las credenciales ingresadas son incorrectas o la cuenta está inactiva.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? normalizedEmail,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("Usuario {Email} inició sesión con éxito.", model.Email);
            return await RedirectAfterLoginAsync(user, returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(model);
    }

    // ==========================================
    // 2. REGISTRO PÚBLICO (REGISTRATION)
    // ==========================================
    [HttpGet]
    public IActionResult Register(string? plan = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new RegisterViewModel
        {
            PlanInteres = string.IsNullOrWhiteSpace(plan) ? "free" : plan.ToLowerInvariant()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        // 1. Validar que el email no exista previamente
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "El correo electrónico ya se encuentra registrado.");
            return View(model);
        }

        // 2. Crear usuario de Identity
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // 3. REGLA DE ORO DE SEGURIDAD (Server-Side):
        // El registro público SIEMPRE asigna el rol 'Member' (prohibido asignar roles administrativos).
        await _userManager.AddToRoleAsync(user, "Member");

        // 4. Asignar Membresía Free con Trial de 24 horas exactas
        var freeMembership = await _context.Memberships.FirstOrDefaultAsync(m => m.Name == "Free")
            ?? await _context.Memberships.FirstAsync();

        var memberProfile = new MemberProfile
        {
            UserId = user.Id,
            MembershipId = freeMembership.Id,
            Height = model.Height,
            BirthDate = model.BirthDate,
            TrialEndsAt = DateTime.UtcNow.AddHours(24), // 24 horas exactas de trial
            CreatedAt = DateTime.UtcNow
        };

        _context.MemberProfiles.Add(memberProfile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nuevo socio registrado: {Email} con plan Free (Trial 24h).", user.Email);

        // 5. Iniciar sesión automáticamente por cookies
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["MensajeExito"] = "¡Bienvenido a FitNet! Tu prueba gratuita de 24 horas está activa.";
        return RedirectToAction("Dashboard", "Members");
    }

    // ==========================================
    // 3. CIERRE DE SESIÓN (LOGOUT)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Usuario cerró sesión.");
        return RedirectToAction("Index", "Home");
    }

    // ==========================================
    // 4. MI PERFIL (PROFILE)
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Member";

        var memberProfile = await _context.MemberProfiles
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var trainerProfile = await _context.TrainerProfiles
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        var pName = memberProfile?.Membership?.Name ?? "Free";
        var isTrActive = memberProfile != null && memberProfile.TrialEndsAt > DateTime.UtcNow;
        var trHours = isTrActive && memberProfile != null ? Math.Max(0, (int)Math.Ceiling((memberProfile.TrialEndsAt - DateTime.UtcNow).TotalHours)) : 0;
        var dRemaining = isTrActive && pName != "Free" && memberProfile != null ? Math.Max(1, (int)Math.Ceiling((memberProfile.TrialEndsAt - DateTime.UtcNow).TotalDays)) : 0;

        ViewData["PlanName"] = pName;
        ViewData["IsTrialActive"] = isTrActive;
        ViewData["TrialHoursRemaining"] = trHours;
        ViewData["DaysRemaining"] = dRemaining;
        ViewData["ExpirationDate"] = memberProfile?.TrialEndsAt.ToString("dd/MM/yyyy") ?? "";

        var model = new ProfileViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            AvatarPath = user.AvatarPath,
            Role = primaryRole,
            PlanName = memberProfile?.Membership?.Name ?? "No aplica",
            TrialEndsAt = memberProfile?.TrialEndsAt,
            Height = memberProfile?.Height,
            BirthDate = memberProfile?.BirthDate,
            Specialty = trainerProfile?.Specialty,
            Biography = trainerProfile?.Biography
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        // Si se envió un archivo de avatar, validarlo con FileStorageService
        if (model.AvatarFile != null)
        {
            if (!_fileStorageService.ValidateImage(model.AvatarFile, out var avatarError))
            {
                ModelState.AddModelError("AvatarFile", avatarError ?? "Archivo de imagen no válido.");
            }
        }

        if (!ModelState.IsValid)
        {
            // Restaurar datos de solo lectura para la vista
            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "Member";
            model.Email = user.Email ?? string.Empty;
            model.AvatarPath = user.AvatarPath;

            var memberProf = await _context.MemberProfiles
                .Include(m => m.Membership)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);
            model.PlanName = memberProf?.Membership?.Name ?? "No aplica";
            model.TrialEndsAt = memberProf?.TrialEndsAt;

            return View(model);
        }

        // Procesar subida de nuevo Avatar si se adjuntó
        if (model.AvatarFile != null)
        {
            try
            {
                // 1. Guardar nuevo avatar con nombre GUID seguro
                var newAvatarPath = await _fileStorageService.SaveFileAsync(model.AvatarFile, "avatars");

                // 2. Si tenía un avatar previo, eliminarlo del disco para evitar basura
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    await _fileStorageService.DeleteFileAsync(user.AvatarPath);
                }

                user.AvatarPath = newAvatarPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la subida del avatar para el usuario {UserId}", user.Id);
                ModelState.AddModelError("AvatarFile", "Ocurrió un error al guardar la imagen de perfil.");
                return View(model);
            }
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();

        await _userManager.UpdateAsync(user);

        // Si es socio, actualizar datos físicos
        var memberProfile = await _context.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (memberProfile != null)
        {
            memberProfile.Height = model.Height;
            memberProfile.BirthDate = model.BirthDate;
            await _context.SaveChangesAsync();
        }

        TempData["MensajeExito"] = "Perfil actualizado correctamente.";
        return RedirectToAction("Profile");
    }

    // ==========================================
    // 5. CAMBIO DE CONTRASEÑA
    // ==========================================
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["MensajeError"] = "Por favor revisá los datos ingresados para el cambio de contraseña.";
            return RedirectToAction("Profile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["MensajeExito"] = "Tu contraseña ha sido actualizada con éxito.";
        }
        else
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Error al actualizar la contraseña.";
            TempData["MensajeError"] = error;
        }

        return RedirectToAction("Profile");
    }

    // ==========================================
    // 6. ACCESO DENEGADO (ACCESS DENIED)
    // ==========================================
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ==========================================
    // HELPERS
    // ==========================================
    private async Task<IActionResult> RedirectAfterLoginAsync(ApplicationUser user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Owner"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (await _userManager.IsInRoleAsync(user, "Trainer"))
        {
            return RedirectToAction("Dashboard", "Trainers");
        }

        return RedirectToAction("Dashboard", "Members");
    }
}
