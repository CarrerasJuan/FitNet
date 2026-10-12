using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FitNet.Data;
using FitNet.DTOs.Auth;
using FitNet.Models;
using FitNet.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FitNet.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IMembershipService _membershipService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IMembershipService membershipService,
        IConfiguration configuration,
        ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _membershipService = membershipService;
        _configuration = configuration;
        _logger = logger;
    }

    // ==========================================
    // 1. POST /api/auth/login
    // Valida credenciales y emite token JWT
    // ==========================================
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Intento fallido de login API: usuario {Email} no existe o está inactivo.", request.Email);
            return Unauthorized(new { message = "Credenciales inválidas o cuenta inactiva." });
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!passwordCheck.Succeeded)
        {
            _logger.LogWarning("Intento fallido de login API: contraseña incorrecta para {Email}.", request.Email);
            return Unauthorized(new { message = "Credenciales inválidas o cuenta inactiva." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Member";

        // Obtener estado consolidado de suscripción mediante MembershipService
        var status = await _membershipService.GetMemberStatusAsync(user.Id);

        // Generar Token JWT
        var (token, expiresAt) = GenerateJwtToken(user, roles);

        var response = new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserSummaryDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarPath = user.AvatarPath,
                Role = primaryRole,
                PlanName = status.PlanName,
                IsTrialActive = status.IsTrial && status.IsActive
            }
        };

        _logger.LogInformation("Login exitoso en API para usuario {Email} (Rol: {Role}). Token generado.", user.Email, primaryRole);
        return Ok(response);
    }

    // ==========================================
    // 2. GET /api/auth/profile
    // Endpoint protegido con Bearer Token
    // ==========================================
    [HttpGet("profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        // Regla de Oro: El usuario autenticado SIEMPRE se obtiene de los Claims del JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Identificador de usuario no presente en el token." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return NotFound(new { message = "Usuario no encontrado o inactivo." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Member";

        // Obtener perfil específico según rol
        var memberProfile = await _context.MemberProfiles
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        var trainerProfile = await _context.TrainerProfiles
            .FirstOrDefaultAsync(t => t.UserId == user.Id);

        var accessStatus = await _membershipService.GetMemberStatusAsync(user.Id);

        var profileDto = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AvatarPath = user.AvatarPath,
            CreatedAt = user.CreatedAt,
            Role = primaryRole,
            Height = memberProfile?.Height,
            BirthDate = memberProfile?.BirthDate,
            Specialty = trainerProfile?.Specialty,
            Biography = trainerProfile?.Biography,
            AccessStatus = accessStatus
        };

        return Ok(profileDto);
    }

    // ==========================================
    // HELPERS: Generación de JWT
    // ==========================================
    private (string token, DateTime expiresAt) GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "FitNet_Super_Secret_Jwt_Security_Key_2026_Development_Key_ULP!";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "FitNetServer";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "FitNetClient";
        var durationMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Agregar roles a los claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return (tokenString, expiresAt);
    }
}
