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
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("SeedService");

        // 0. Garantizar Creación de Base de Datos y Tablas en MySQL si se levanta limpia
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger?.LogInformation("Esquema relacional verificado: {Message}", ex.Message);
        }

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
        var premiumMembership = await context.Memberships.FirstAsync(m => m.Name == "Premium");
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
            var newTrainer = new ApplicationUser
            {
                UserName = "trainer@fitnet.com",
                Email = "trainer@fitnet.com",
                FirstName = "Lucas",
                LastName = "Vázquez",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(newTrainer, "Trainer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newTrainer, "Trainer");
                context.TrainerProfiles.Add(new TrainerProfile
                {
                    UserId = newTrainer.Id,
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

        // 3.6. Usuario Member (Premium Activo con Coach Asignado)
        if (await userManager.FindByEmailAsync("premium@fitnet.com") == null)
        {
            var premiumMember = new ApplicationUser
            {
                UserName = "premium@fitnet.com",
                Email = "premium@fitnet.com",
                FirstName = "Sofía",
                LastName = "Atleta",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(premiumMember, "Premium123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(premiumMember, "Member");
                context.MemberProfiles.Add(new MemberProfile
                {
                    UserId = premiumMember.Id,
                    MembershipId = premiumMembership.Id,
                    Height = 168,
                    BirthDate = new DateTime(1999, 11, 22),
                    TrialEndsAt = DateTime.UtcNow.AddYears(1)
                });
                await context.SaveChangesAsync();
            }
        }

        // 3.7. Usuario Member (Trial Vencido - para Evaluación Inmediata de Seguridad)
        if (await userManager.FindByEmailAsync("expired@fitnet.com") == null)
        {
            var expiredMember = new ApplicationUser
            {
                UserName = "expired@fitnet.com",
                Email = "expired@fitnet.com",
                FirstName = "Tomás",
                LastName = "Vencido",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(expiredMember, "Expired123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(expiredMember, "Member");
                context.MemberProfiles.Add(new MemberProfile
                {
                    UserId = expiredMember.Id,
                    MembershipId = freeMembership.Id,
                    Height = 180,
                    BirthDate = new DateTime(2000, 3, 10),
                    TrialEndsAt = DateTime.UtcNow.AddDays(-2) // Expirado hace 48hs
                });
                await context.SaveChangesAsync();
            }
        }

        // 4. Asegurar Catálogo Inicial de Ejercicios
        if (!await context.Exercises.AnyAsync())
        {
            context.Exercises.AddRange(
                // Ejercicios Públicos (Visibles en Trial Free y Planes Superiores)
                new Exercise
                {
                    Name = "Sentadilla con Peso Corporal",
                    Description = "Ejercicio básico de tren inferior enfocado en cuádriceps, glúteos y core. Mantener la espalda recta y romper el paralelo.",
                    MuscleGroup = "Piernas",
                    Difficulty = "Principiante",
                    IsPublic = true,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Flexiones de Brazos (Push Ups)",
                    Description = "Trabajo de empuje horizontal para pectoral mayor, tríceps y deltoides anterior con el propio peso.",
                    MuscleGroup = "Pecho",
                    Difficulty = "Principiante",
                    IsPublic = true,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Plancha Abdominal Isométrica",
                    Description = "Fortalecimiento del transverso abdominal, core y estabilidad de columna lumbopélvica.",
                    MuscleGroup = "Core",
                    Difficulty = "Principiante",
                    IsPublic = true,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Estocadas / Zancadas Dinámicas",
                    Description = "Ejercicio unilateral de piernas para equilibrio, estabilidad de rodilla, glúteos e isquiotibiales.",
                    MuscleGroup = "Piernas",
                    Difficulty = "Intermedio",
                    IsPublic = true,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Fondos de Tríceps en Banco",
                    Description = "Extensión de codo para desarrollo de tríceps utilizando elevación de apoyo.",
                    MuscleGroup = "Brazos",
                    Difficulty = "Principiante",
                    IsPublic = true,
                    IsActive = true
                },
                // Ejercicios Exclusivos (Biblioteca Completa — Solo Gold y Premium)
                new Exercise
                {
                    Name = "Press de Banca Plano con Barra",
                    Description = "Ejercicio compuesto de fuerza máxima y sobrecarga progresiva para desarrollo del pectoral.",
                    MuscleGroup = "Pecho",
                    Difficulty = "Intermedio",
                    IsPublic = false,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Peso Muerto Rumano con Barra",
                    Description = "Bisagra de cadera fundamental para cadena posterior: isquiosurales, glúteos y erectores espinales.",
                    MuscleGroup = "Piernas",
                    Difficulty = "Avanzado",
                    IsPublic = false,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Dominadas Pronas Estrictas",
                    Description = "Tracción vertical fundamental para amplitud de dorsal ancho, redondos y bíceps braquial.",
                    MuscleGroup = "Espalda",
                    Difficulty = "Avanzado",
                    IsPublic = false,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Press Militar con Mancuernas",
                    Description = "Empuje vertical para desarrollo de deltoides medio y anterior con estabilización escapular.",
                    MuscleGroup = "Hombros",
                    Difficulty = "Intermedio",
                    IsPublic = false,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Remo con Barra T",
                    Description = "Tracción horizontal pesada para densidad de espalda media, romboides y trapecios.",
                    MuscleGroup = "Espalda",
                    Difficulty = "Intermedio",
                    IsPublic = false,
                    IsActive = true
                },
                new Exercise
                {
                    Name = "Sentadilla Búlgara con Mancuernas",
                    Description = "Ejercicio hipertrófico unilateral de alta intensidad para cuádriceps y glúteo mayor.",
                    MuscleGroup = "Piernas",
                    Difficulty = "Avanzado",
                    IsPublic = false,
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }

        // 5. Asegurar Rutinas y Asignaciones Iniciales
        if (!await context.WorkoutPlans.AnyAsync())
        {
            var trainer = await userManager.FindByEmailAsync("trainer@fitnet.com");
            var sentadilla = await context.Exercises.FirstAsync(e => e.Name.Contains("Sentadilla con"));
            var pressBanca = await context.Exercises.FirstAsync(e => e.Name.Contains("Press de Banca"));
            var dominadas = await context.Exercises.FirstAsync(e => e.Name.Contains("Dominadas"));
            var plancha = await context.Exercises.FirstAsync(e => e.Name.Contains("Plancha"));
            var pesoMuerto = await context.Exercises.FirstAsync(e => e.Name.Contains("Peso Muerto"));
            var pressMilitar = await context.Exercises.FirstAsync(e => e.Name.Contains("Press Militar"));

            // 5.1. Rutina General (Gold / Premium)
            var generalPlan = new WorkoutPlan
            {
                Name = "Rutina General Full Body (Hipertrofia & Base)",
                Description = "Estructura global de cuerpo completo para fuerza básica y estímulo muscular uniforme en 3 días semanales.",
                IsGeneral = true,
                TrainerId = trainer?.Id,
                WorkoutExercises = new List<WorkoutExercise>
                {
                    new() { Exercise = sentadilla, Order = 1, Sets = 4, Repetitions = 10, RestSeconds = 90, Notes = "Bajar controlado y mantener pecho erguido." },
                    new() { Exercise = pressBanca, Order = 2, Sets = 4, Repetitions = 8, RestSeconds = 90, Notes = "Retracción escapular firme durante todo el recorrido." },
                    new() { Exercise = dominadas, Order = 3, Sets = 3, Repetitions = 8, RestSeconds = 75, Notes = "Rango completo de recorrido hasta pasar la barbilla." },
                    new() { Exercise = plancha, Order = 4, Sets = 3, Repetitions = 45, RestSeconds = 60, Notes = "45 segundos por serie con glúteos y abdomen apretados." }
                }
            };

            // 5.2. Rutina Personalizada (Premium) asignada a member@fitnet.com
            var personalizedPlan = new WorkoutPlan
            {
                Name = "Plan de Acondicionamiento de Atleta - Fase 1",
                Description = "Plan de fuerza enfocado en sobrecarga progresiva y cadena posterior diseñado por Coach Lucas.",
                IsGeneral = false,
                TrainerId = trainer?.Id,
                WorkoutExercises = new List<WorkoutExercise>
                {
                    new() { Exercise = pesoMuerto, Order = 1, Sets = 4, Repetitions = 6, RestSeconds = 120, Notes = "Bisagra de cadera estricta. Cero flexión de columna." },
                    new() { Exercise = pressMilitar, Order = 2, Sets = 4, Repetitions = 8, RestSeconds = 90, Notes = "Bloqueo firme arriba sin hiperextender lumbares." },
                    new() { Exercise = dominadas, Order = 3, Sets = 4, Repetitions = 6, RestSeconds = 90, Notes = "Con pausa isométrica de 1 segundo arriba." }
                }
            };

            context.WorkoutPlans.AddRange(generalPlan, personalizedPlan);
            await context.SaveChangesAsync();
        }

        // 6. Asegurar Asignaciones y Registros de Progreso Iniciales
        var coachTrainer = await userManager.FindByEmailAsync("trainer@fitnet.com");
        var activePersonalizedPlan = await context.WorkoutPlans.FirstOrDefaultAsync(wp => !wp.IsGeneral);

        if (activePersonalizedPlan != null)
        {
            // 6.1. Asignación activa a member@fitnet.com (Gold)
            var memberUser = await userManager.FindByEmailAsync("member@fitnet.com");
            if (memberUser != null)
            {
                var memberProfile = await context.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == memberUser.Id);
                if (memberProfile != null && !await context.WorkoutAssignments.AnyAsync(a => a.MemberId == memberProfile.Id))
                {
                    context.WorkoutAssignments.Add(new WorkoutAssignment
                    {
                        MemberId = memberProfile.Id,
                        WorkoutPlanId = activePersonalizedPlan.Id,
                        AssignedByTrainerId = coachTrainer?.Id,
                        StartDate = DateTime.UtcNow.Date.AddDays(-14),
                        IsActive = true
                    });
                    await context.SaveChangesAsync();

                    if (!await context.ProgressRecords.AnyAsync(p => p.MemberId == memberProfile.Id))
                    {
                        context.ProgressRecords.AddRange(
                            new ProgressRecord
                            {
                                MemberId = memberProfile.Id,
                                Date = DateTime.UtcNow.Date.AddDays(-14),
                                Weight = 78.5m,
                                BodyFat = 16.0m,
                                Notes = "Inicio de ciclo de fuerza con Coach Lucas. Adaptación positiva en sentadillas.",
                                CreatedAt = DateTime.UtcNow.AddDays(-14)
                            },
                            new ProgressRecord
                            {
                                MemberId = memberProfile.Id,
                                Date = DateTime.UtcNow.Date.AddDays(-2),
                                Weight = 77.2m,
                                BodyFat = 15.3m,
                                Notes = "Control quincenal. Descenso de 1.3 kg de grasa y mejoras en dominadas estrictas.",
                                CreatedAt = DateTime.UtcNow.AddDays(-2)
                            }
                        );
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 6.2. Asignación activa a premium@fitnet.com (Premium con Coach)
            var premiumUser = await userManager.FindByEmailAsync("premium@fitnet.com");
            if (premiumUser != null)
            {
                var premiumProfile = await context.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == premiumUser.Id);
                if (premiumProfile != null && !await context.WorkoutAssignments.AnyAsync(a => a.MemberId == premiumProfile.Id))
                {
                    context.WorkoutAssignments.Add(new WorkoutAssignment
                    {
                        MemberId = premiumProfile.Id,
                        WorkoutPlanId = activePersonalizedPlan.Id,
                        AssignedByTrainerId = coachTrainer?.Id,
                        StartDate = DateTime.UtcNow.Date.AddDays(-30),
                        IsActive = true
                    });
                    await context.SaveChangesAsync();

                    if (!await context.ProgressRecords.AnyAsync(p => p.MemberId == premiumProfile.Id))
                    {
                        context.ProgressRecords.AddRange(
                            new ProgressRecord
                            {
                                MemberId = premiumProfile.Id,
                                Date = DateTime.UtcNow.Date.AddDays(-30),
                                Weight = 62.5m,
                                BodyFat = 23.0m,
                                Notes = "Evaluación inicial y diseño de plan personalizado por Coach Lucas.",
                                CreatedAt = DateTime.UtcNow.AddDays(-30)
                            },
                            new ProgressRecord
                            {
                                MemberId = premiumProfile.Id,
                                Date = DateTime.UtcNow.Date.AddDays(-10),
                                Weight = 61.2m,
                                BodyFat = 21.8m,
                                Notes = "Excelente adherencia al plan de fuerza y reducción de porcentaje graso.",
                                CreatedAt = DateTime.UtcNow.AddDays(-10)
                            }
                        );
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
