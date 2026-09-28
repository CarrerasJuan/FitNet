using FitNet.Data;
using FitNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Services;

public static class SeedService
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Asegurar Roles de Seguridad
        string[] roles = ["Owner", "Admin", "Trainer", "Member"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Asegurar Membresías Comerciales
        if (!await context.Memberships.AnyAsync())
        {
            context.Memberships.AddRange(
                new Membership
                {
                    Name = "Free",
                    Description = "Trial de 24 horas con acceso a perfil y ejercicios públicos.",
                    MonthlyPrice = 0,
                    SemiannualPrice = 0,
                    IsActive = true
                },
                new Membership
                {
                    Name = "Gold",
                    Description = "Acceso ilimitado a biblioteca completa de ejercicios, rutinas generales del gimnasio y registro de progreso.",
                    MonthlyPrice = 32000,
                    SemiannualPrice = 25600,
                    IsActive = true
                },
                new Membership
                {
                    Name = "Premium",
                    Description = "Todo Gold + Entrenador personal asignado, rutinas 100% personalizadas y seguimiento continuo.",
                    MonthlyPrice = 65000,
                    SemiannualPrice = 52000,
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }

        var goldMembership = await context.Memberships.FirstAsync(m => m.Name == "Gold");
        var freeMembership = await context.Memberships.FirstAsync(m => m.Name == "Free");

        // 3. Usuarios de Prueba para cada Rol (Requerimiento Académico)
        
        // 3.1. Usuario Owner
        if (await userManager.FindByEmailAsync("owner@fitnet.com") == null)
        {
            var owner = new ApplicationUser
            {
                UserName = "owner@fitnet.com",
                Email = "owner@fitnet.com",
                FirstName = "Guillermo",
                LastName = "Propietario",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(owner, "Owner123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(owner, "Owner");
            }
        }

        // 3.2. Usuario Admin
        if (await userManager.FindByEmailAsync("admin@fitnet.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@fitnet.com",
                Email = "admin@fitnet.com",
                FirstName = "Carlos",
                LastName = "Administrador",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // 3.3. Usuario Trainer
        if (await userManager.FindByEmailAsync("trainer@fitnet.com") == null)
        {
            var trainer = new ApplicationUser
            {
                UserName = "trainer@fitnet.com",
                Email = "trainer@fitnet.com",
                FirstName = "Lucas",
                LastName = "Vázquez",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(trainer, "Trainer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(trainer, "Trainer");
                context.TrainerProfiles.Add(new TrainerProfile
                {
                    UserId = trainer.Id,
                    Specialty = "Hipertrofia & Fuerza",
                    Biography = "Licenciado en Educación Física especializado en biomecánica y sobrecarga progresiva."
                });
                await context.SaveChangesAsync();
            }
        }

        // 3.4. Usuario Member (Gold Activo)
        if (await userManager.FindByEmailAsync("member@fitnet.com") == null)
        {
            var member = new ApplicationUser
            {
                UserName = "member@fitnet.com",
                Email = "member@fitnet.com",
                FirstName = "Federico",
                LastName = "Socio",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(member, "Member123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(member, "Member");
                context.MemberProfiles.Add(new MemberProfile
                {
                    UserId = member.Id,
                    MembershipId = goldMembership.Id,
                    Height = 178,
                    BirthDate = new DateTime(1998, 5, 14),
                    TrialEndsAt = DateTime.UtcNow.AddYears(1)
                });
                await context.SaveChangesAsync();
            }
        }

        // 3.5. Usuario Member (Trial 24h)
        if (await userManager.FindByEmailAsync("trial@fitnet.com") == null)
        {
            var trialMember = new ApplicationUser
            {
                UserName = "trial@fitnet.com",
                Email = "trial@fitnet.com",
                FirstName = "Juan",
                LastName = "Prueba",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(trialMember, "Trial123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(trialMember, "Member");
                context.MemberProfiles.Add(new MemberProfile
                {
                    UserId = trialMember.Id,
                    MembershipId = freeMembership.Id,
                    Height = 175,
                    BirthDate = new DateTime(2001, 8, 20),
                    TrialEndsAt = DateTime.UtcNow.AddHours(24) // 24 horas exactas de prueba
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
