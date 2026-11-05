using System.Text;
using System.Text.Json;
using FitNet.Data;
using FitNet.Middlewares;
using FitNet.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Conexión a Base de Datos MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=FitNetDb;User=root;Password=;CharSet=utf8mb4;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

// Inyección de Dependencias de Servicios de FitNet
builder.Services.AddScoped<FitNet.Services.IFileStorageService, FitNet.Services.FileStorageService>();
builder.Services.AddScoped<FitNet.Services.IMembershipService, FitNet.Services.MembershipService>();
builder.Services.AddScoped<FitNet.Services.IMetricsService, FitNet.Services.MetricsService>();

// Configuración de Diagnóstico y Health Checks (Resiliencia y Monitoreo de BD)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "MySQL - FitNetDb",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "mysql", "persistence" });

// Configuración de ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Requisitos de contraseña para desarrollo
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuración de Cookies de Identity para MVC
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "FitNet.AuthCookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Configuración de Cookies Antiforgery (Protección CSRF)
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "FitNet.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configuración de Autenticación JWT Bearer para la API REST
var jwtKey = builder.Configuration["Jwt:Key"] ?? "FitNet_Super_Secret_Jwt_Security_Key_2026_Development_Key_ULP!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FitNetServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FitNetClient";

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// MVC Controllers con Vistas y API Controllers
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 1. Middleware Global de Excepciones y Resiliencia (RFC 7807 para API, Vista para MVC)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cabeceras HTTP de Seguridad Defensiva (OWASP / Blindaje HTTP)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Manejo unificado de códigos de estado (404/403 no controlados)
app.UseStatusCodePages(async statusCodeContext =>
{
    var req = statusCodeContext.HttpContext.Request;
    var isApi = req.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        || (req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !req.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase));

    var status = statusCodeContext.HttpContext.Response.StatusCode;

    if (isApi)
    {
        statusCodeContext.HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = status switch
            {
                401 => "No autenticado",
                403 => "Acceso prohibido",
                404 => "Recurso no encontrado",
                _ => "Error HTTP"
            },
            Status = status,
            Detail = status switch
            {
                401 => "Se requiere un token JWT Bearer válido en el encabezado Authorization.",
                403 => "El usuario autenticado no posee los permisos o roles requeridos para este recurso.",
                404 => $"El endpoint '{req.Path}' no existe en la API REST de FitNet.",
                _ => $"La solicitud finalizó con código de estado HTTP {status}."
            },
            Instance = req.Path
        };
        problem.Extensions["traceId"] = statusCodeContext.HttpContext.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        await statusCodeContext.HttpContext.Response.WriteAsync(json);
    }
    else
    {
        var redirectUrl = $"/Home/Error?statusCode={status}";
        statusCodeContext.HttpContext.Response.Redirect(redirectUrl);
    }
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Endpoint de Diagnóstico y Salud del Sistema (/health)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var result = new
        {
            status = report.Status.ToString(),
            system = "FitNet Gym Management Platform",
            version = "1.0.0",
            timestamp = DateTime.UtcNow.ToString("o"),
            totalDuration = $"{report.TotalDuration.TotalMilliseconds:0.##} ms",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = $"{e.Value.Duration.TotalMilliseconds:0.##} ms",
                description = e.Value.Description ?? "Verificación de conectividad activa contra base de datos MySQL (FitNetDb)",
                error = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
    }
}).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Ejecutar Seed Inicial (Roles, Membresías y Usuarios de desarrollo)
await FitNet.Services.SeedService.SeedAsync(app.Services);

app.Run();
