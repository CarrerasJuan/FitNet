using System.Security.Claims;
using FitNet.Data;
using FitNet.Models;
using FitNet.ViewModels.WorkoutPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Controllers;

[Authorize(Roles = "Trainer,Admin,Owner")]
public class WorkoutPlansController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WorkoutPlansController> _logger;

    public WorkoutPlansController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<WorkoutPlansController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 1. GET /WorkoutPlans
    // Listado paginado server-side con filtros
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(string? filter = "all", string? search = null, int page = 1, int pageSize = 9)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isTrainer = User.IsInRole("Trainer");
        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        // Normalización de paginado
        if (page < 1) page = 1;
        if (pageSize < 3 || pageSize > 30) pageSize = 9;

        var query = _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Trainer)
            .Include(p => p.WorkoutExercises)
            .Include(p => p.Assignments.Where(a => a.IsActive))
            .AsQueryable();

        // Filtro por alcance
        if (filter == "general")
        {
            query = query.Where(p => p.IsGeneral);
        }
        else if (filter == "mine")
        {
            query = query.Where(p => p.TrainerId == user.Id);
        }

        // Búsqueda textual
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        // Conteo total para paginado
        var totalItems = await query.CountAsync();

        // Paginado Skip / Take directo en MySQL
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new WorkoutPlanItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsGeneral = p.IsGeneral,
                TrainerId = p.TrainerId,
                TrainerName = p.Trainer != null ? $"{p.Trainer.FirstName} {p.Trainer.LastName}" : "Staff FitNet",
                TrainerAvatar = p.Trainer != null ? p.Trainer.AvatarPath : null,
                ExercisesCount = p.WorkoutExercises.Count,
                ActiveAssignmentsCount = p.Assignments.Count(a => a.IsActive),
                CreatedAt = p.CreatedAt,
                CanEditOrDelete = isAdminOrOwner || (p.TrainerId == user.Id)
            })
            .ToListAsync();

        // Métricas KPI de cabecera
        var totalPlans = await _context.WorkoutPlans.CountAsync();
        var generalPlans = await _context.WorkoutPlans.CountAsync(p => p.IsGeneral);
        var personalPlans = totalPlans - generalPlans;

        var model = new WorkoutPlanIndexViewModel
        {
            Plans = items,
            Filter = filter ?? "all",
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPlansCount = totalPlans,
            GeneralPlansCount = generalPlans,
            PersonalPlansCount = personalPlans,
            IsTrainer = isTrainer,
            IsAdminOrOwner = isAdminOrOwner
        };

        return View(model);
    }

    // ==========================================
    // 2. GET /WorkoutPlans/Details/{id}
    // Ficha técnica completa de la rutina
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Trainer)
                .ThenInclude(t => t!.TrainerProfile)
            .Include(p => p.WorkoutExercises.OrderBy(we => we.Order))
                .ThenInclude(we => we.Exercise)
            .Include(p => p.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Member)
                    .ThenInclude(m => m.User)
            .Include(p => p.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Member)
                    .ThenInclude(m => m.Membership)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            TempData["MensajeError"] = "La rutina solicitada no fue encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var model = new WorkoutPlanDetailsViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            IsGeneral = plan.IsGeneral,
            CreatedAt = plan.CreatedAt,
            TrainerId = plan.TrainerId,
            TrainerName = plan.Trainer != null ? $"{plan.Trainer.FirstName} {plan.Trainer.LastName}" : "Staff Oficial FitNet",
            TrainerSpecialty = plan.Trainer?.TrainerProfile?.Specialty ?? "Entrenamiento Funcional",
            TrainerEmail = plan.Trainer?.Email,
            TrainerAvatar = plan.Trainer?.AvatarPath,
            CanEditOrDelete = isAdminOrOwner || (plan.TrainerId == user.Id),
            Exercises = plan.WorkoutExercises.Select(we => new WorkoutPlanExerciseDetailItemViewModel
            {
                Order = we.Order,
                ExerciseId = we.ExerciseId,
                ExerciseName = we.Exercise.Name,
                Description = we.Exercise.Description,
                MuscleGroup = we.Exercise.MuscleGroup,
                Difficulty = we.Exercise.Difficulty,
                ImagePath = we.Exercise.ImagePath,
                VideoUrl = we.Exercise.VideoUrl,
                Sets = we.Sets,
                Repetitions = we.Repetitions,
                RestSeconds = we.RestSeconds,
                Notes = we.Notes
            }).ToList(),
            AssignedAthletes = plan.Assignments.Select(a => new WorkoutPlanAssignedAthleteViewModel
            {
                AssignmentId = a.Id,
                MemberProfileId = a.MemberId,
                FullName = $"{a.Member.User.FirstName} {a.Member.User.LastName}",
                Email = a.Member.User.Email ?? string.Empty,
                AvatarPath = a.Member.User.AvatarPath,
                PlanName = a.Member.Membership.Name,
                StartDate = a.StartDate,
                IsActive = a.IsActive
            }).ToList()
        };

        return View(model);
    }

    // ==========================================
    // 3. GET /WorkoutPlans/Create
    // Creador interactivo de rutinas
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Create(int? memberId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var model = new WorkoutPlanCreateViewModel
        {
            CanSetGeneral = isAdminOrOwner,
            IsGeneral = false
        };

        await PopulateCreateAvailableMembersAsync(model);

        // Si se recibe memberId, pre-cargar datos del alumno asignado
        if (memberId.HasValue && memberId.Value > 0)
        {
            var member = model.AvailableMembers.FirstOrDefault(m => m.Id == memberId.Value);
            if (member != null)
            {
                model.MemberProfileId = member.Id;
                model.MemberName = member.FullName;
                model.MemberEmail = member.Email;
                model.MemberAvatar = member.AvatarPath;
                model.Name = string.Empty;
                model.Description = string.Empty;
            }
        }

        return View(model);
    }

    // ==========================================
    // 4. POST /WorkoutPlans/Create
    // Creación segura con validación server-side
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkoutPlanCreateViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        // Regla Defensiva de Seguridad: Un entrenador jamás puede forzar una rutina oficial general
        if (!isAdminOrOwner)
        {
            model.IsGeneral = false;
        }

        // Si el nombre no fue especificado, asignar uno representativo por defecto
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            if (model.MemberProfileId.HasValue && model.MemberProfileId.Value > 0)
            {
                var assignedMember = await _context.MemberProfiles
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == model.MemberProfileId.Value);

                model.Name = assignedMember != null
                    ? $"Rutina - {assignedMember.User.FirstName} {assignedMember.User.LastName}"
                    : $"Rutina Personalizada - {DateTime.UtcNow:dd/MM/yyyy}";
            }
            else
            {
                model.Name = $"Rutina General - {DateTime.UtcNow:dd/MM/yyyy}";
            }
            ModelState.Remove(nameof(model.Name));
        }

        // Validación Server-Side obligatoria: al menos 1 ejercicio prescrito
        if (model.Exercises == null || model.Exercises.Count == 0)
        {
            ModelState.AddModelError("Exercises", "Debes prescribir al menos un ejercicio en la rutina.");
        }
        else
        {
            // Validar que todos los ejercicios seleccionados existan y estén activos en MySQL
            var exerciseIds = model.Exercises.Select(e => e.ExerciseId).Distinct().ToList();
            var validExercises = await _context.Exercises
                .Where(e => exerciseIds.Contains(e.Id) && e.IsActive)
                .ToDictionaryAsync(e => e.Id);

            if (validExercises.Count != exerciseIds.Count)
            {
                ModelState.AddModelError("Exercises", "Uno o más ejercicios seleccionados no son válidos o se encuentran inactivos.");
            }
            else
            {
                // Repoblar metadatos visuales por si hay error en otro campo del formulario
                foreach (var exInput in model.Exercises)
                {
                    if (validExercises.TryGetValue(exInput.ExerciseId, out var exData))
                    {
                        exInput.ExerciseName = exData.Name;
                        exInput.MuscleGroup = exData.MuscleGroup;
                        exInput.Difficulty = exData.Difficulty;
                        exInput.ImagePath = exData.ImagePath;
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            model.CanSetGeneral = isAdminOrOwner;
            await PopulateCreateAvailableMembersAsync(model);
            if (model.MemberProfileId.HasValue && model.MemberProfileId.Value > 0)
            {
                var member = model.AvailableMembers.FirstOrDefault(m => m.Id == model.MemberProfileId.Value);
                if (member != null)
                {
                    model.MemberName = member.FullName;
                    model.MemberEmail = member.Email;
                    model.MemberAvatar = member.AvatarPath;
                }
            }
            return View(model);
        }

        // 1. Crear la Rutina (WorkoutPlan)
        var plan = new WorkoutPlan
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            IsGeneral = model.IsGeneral,
            TrainerId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Asociar los Ejercicios Prescriptos (WorkoutExercises)
        int orderSeq = 1;
        foreach (var ex in model.Exercises!.OrderBy(e => e.Order))
        {
            plan.WorkoutExercises.Add(new WorkoutExercise
            {
                ExerciseId = ex.ExerciseId,
                Order = orderSeq++,
                Sets = ex.Sets,
                Repetitions = ex.Repetitions,
                RestSeconds = ex.RestSeconds,
                Notes = ex.Notes?.Trim()
            });
        }

        _context.WorkoutPlans.Add(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rutina '{Name}' (ID: {Id}) creada por usuario {User}.", 
            plan.Name, plan.Id, user.Email);

        // 3. Si se prescribió a un socio específico, crear asignación activa (WorkoutAssignment)
        if (model.MemberProfileId.HasValue && model.MemberProfileId.Value > 0)
        {
            var member = await _context.MemberProfiles
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == model.MemberProfileId.Value);

            if (member != null)
            {
                // Desactivar asignación previa de esta misma rutina si ya existía
                var oldAssignments = await _context.WorkoutAssignments
                    .Where(a => a.MemberId == member.Id && a.WorkoutPlanId == plan.Id && a.IsActive)
                    .ToListAsync();

                foreach (var old in oldAssignments)
                {
                    old.IsActive = false;
                }

                var assignment = new WorkoutAssignment
                {
                    MemberId = member.Id,
                    WorkoutPlanId = plan.Id,
                    AssignedByTrainerId = user.Id,
                    AssignedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true
                };

                _context.WorkoutAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rutina ID {PlanId} asignada al socio Profile ID {MemberId}.", 
                    plan.Id, member.Id);

                TempData["MensajeExito"] = $"Rutina '{plan.Name}' creada y asignada exitosamente a {member.User.FirstName} {member.User.LastName}.";
                return RedirectToAction(nameof(Details), new { id = plan.Id });
            }
        }

        TempData["MensajeExito"] = $"Rutina '{plan.Name}' creada exitosamente con {plan.WorkoutExercises.Count} ejercicios.";
        return RedirectToAction(nameof(Details), new { id = plan.Id });
    }

    // ==========================================
    // 4.1. GET /WorkoutPlans/Edit/{id}
    // Edición interactiva de rutina ("Seguir Editando")
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var plan = await _context.WorkoutPlans
            .Include(p => p.WorkoutExercises.OrderBy(we => we.Order))
                .ThenInclude(we => we.Exercise)
            .Include(p => p.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Member)
                    .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            TempData["MensajeError"] = "La rutina no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (!isAdminOrOwner && plan.TrainerId != user.Id)
        {
            TempData["MensajeError"] = "No tienes permisos para editar una rutina creada por otro entrenador.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var activeAssignment = plan.Assignments.FirstOrDefault(a => a.IsActive);

        var model = new WorkoutPlanCreateViewModel
        {
            Name = plan.Name,
            Description = plan.Description,
            IsGeneral = plan.IsGeneral,
            CanSetGeneral = isAdminOrOwner,
            MemberProfileId = activeAssignment?.MemberId,
            MemberName = activeAssignment != null ? $"{activeAssignment.Member.User.FirstName} {activeAssignment.Member.User.LastName}" : null,
            MemberEmail = activeAssignment?.Member.User.Email,
            MemberAvatar = activeAssignment?.Member.User.AvatarPath,
            Exercises = plan.WorkoutExercises.Select(we => new WorkoutPlanExerciseInputModel
            {
                ExerciseId = we.ExerciseId,
                Order = we.Order,
                Sets = we.Sets,
                Repetitions = we.Repetitions,
                RestSeconds = we.RestSeconds,
                Notes = we.Notes,
                ExerciseName = we.Exercise.Name,
                MuscleGroup = we.Exercise.MuscleGroup,
                Difficulty = we.Exercise.Difficulty,
                ImagePath = we.Exercise.ImagePath
            }).ToList()
        };

        await PopulateCreateAvailableMembersAsync(model);

        ViewBag.IsEditing = true;
        ViewBag.EditPlanId = plan.Id;

        return View("Create", model);
    }

    // ==========================================
    // 4.2. POST /WorkoutPlans/Edit/{id}
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkoutPlanCreateViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var plan = await _context.WorkoutPlans
            .Include(p => p.WorkoutExercises)
            .Include(p => p.Assignments.Where(a => a.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            TempData["MensajeError"] = "La rutina que intentas editar no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (!isAdminOrOwner && plan.TrainerId != user.Id)
        {
            TempData["MensajeError"] = "No tienes permisos para editar esta rutina.";
            return RedirectToAction(nameof(Index));
        }

        if (!isAdminOrOwner)
        {
            model.IsGeneral = false;
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            model.Name = plan.Name;
            ModelState.Remove(nameof(model.Name));
        }

        if (model.Exercises == null || model.Exercises.Count == 0)
        {
            ModelState.AddModelError("Exercises", "Debes prescribir al menos un ejercicio en la rutina.");
        }

        if (!ModelState.IsValid)
        {
            model.CanSetGeneral = isAdminOrOwner;
            await PopulateCreateAvailableMembersAsync(model);
            ViewBag.IsEditing = true;
            ViewBag.EditPlanId = plan.Id;
            return View("Create", model);
        }

        // Actualizar datos del plan
        plan.Name = model.Name.Trim();
        plan.Description = model.Description?.Trim();
        plan.IsGeneral = model.IsGeneral;

        // Actualizar ejercicios prescriptos
        _context.WorkoutExercises.RemoveRange(plan.WorkoutExercises);

        int orderSeq = 1;
        foreach (var ex in model.Exercises!.OrderBy(e => e.Order))
        {
            plan.WorkoutExercises.Add(new WorkoutExercise
            {
                ExerciseId = ex.ExerciseId,
                Order = orderSeq++,
                Sets = ex.Sets,
                Repetitions = ex.Repetitions,
                RestSeconds = ex.RestSeconds,
                Notes = ex.Notes?.Trim()
            });
        }

        await _context.SaveChangesAsync();

        // Si se cambió o asignó un socio
        if (model.MemberProfileId.HasValue && model.MemberProfileId.Value > 0)
        {
            var currentAssignment = plan.Assignments.FirstOrDefault(a => a.IsActive);
            if (currentAssignment == null || currentAssignment.MemberId != model.MemberProfileId.Value)
            {
                if (currentAssignment != null)
                {
                    currentAssignment.IsActive = false;
                }

                var newAssignment = new WorkoutAssignment
                {
                    MemberId = model.MemberProfileId.Value,
                    WorkoutPlanId = plan.Id,
                    AssignedByTrainerId = user.Id,
                    AssignedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true
                };
                _context.WorkoutAssignments.Add(newAssignment);
                await _context.SaveChangesAsync();
            }
        }

        TempData["MensajeExito"] = $"Rutina '{plan.Name}' actualizada exitosamente.";
        return RedirectToAction(nameof(Details), new { id = plan.Id });
    }

    // ==========================================
    // 5. POST /WorkoutPlans/Delete/{id}
    // Baja segura con control de asignaciones activas
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var plan = await _context.WorkoutPlans
            .Include(p => p.Assignments.Where(a => a.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            TempData["MensajeError"] = "La rutina que intentas eliminar no existe.";
            return RedirectToAction(nameof(Index));
        }

        // Auditoría por rol
        if (!isAdminOrOwner && plan.TrainerId != user.Id)
        {
            TempData["MensajeError"] = "No tienes permisos para eliminar una rutina creada por otro entrenador.";
            return RedirectToAction(nameof(Index));
        }

        // Control de asignaciones activas: defensa contra inconsistencias
        if (plan.Assignments.Any())
        {
            TempData["MensajeError"] = $"No es posible eliminar la rutina '{plan.Name}' porque está asignada a {plan.Assignments.Count} socio(s) con entrenamiento activo. Desasigna a los socios antes de proceder.";
            return RedirectToAction(nameof(Index));
        }

        _context.WorkoutPlans.Remove(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rutina ID {Id} ('{Name}') eliminada por {User}.", plan.Id, plan.Name, user.Email);
        TempData["MensajeExito"] = $"La rutina '{plan.Name}' fue eliminada exitosamente.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // 6. GET /WorkoutPlans/Assign
    // Formulario de asignación de rutinas a atletas
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Assign(int? planId, int? memberId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var model = new WorkoutAssignmentCreateViewModel
        {
            WorkoutPlanId = planId ?? 0,
            MemberProfileId = memberId ?? 0,
            StartDate = DateTime.UtcNow.Date
        };

        await PopulateAssignmentOptionsAsync(model, user, isAdminOrOwner, planId, memberId);

        return View(model);
    }

    // ==========================================
    // 7. POST /WorkoutPlans/Assign
    // Asignación con validación server-side de membresía
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(WorkoutAssignmentCreateViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
        {
            ModelState.AddModelError("EndDate", "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        var plan = await _context.WorkoutPlans
            .Include(p => p.Trainer)
            .FirstOrDefaultAsync(p => p.Id == model.WorkoutPlanId);

        if (plan == null)
        {
            ModelState.AddModelError("WorkoutPlanId", "La rutina seleccionada no existe.");
        }

        var member = await _context.MemberProfiles
            .Include(m => m.User)
            .Include(m => m.Membership)
            .FirstOrDefaultAsync(m => m.Id == model.MemberProfileId);

        if (member == null)
        {
            ModelState.AddModelError("MemberProfileId", "El alumno seleccionado no existe o no tiene perfil activo.");
        }
        else if (plan != null)
        {
            // REGLAS DEFENSIVAS DE MEMBRESÍA (Requisito Tarea 9.3)
            // 1. Membresía Free (Trial 24h) nunca accede a rutinas guiadas
            if (member.Membership.Name == "Free")
            {
                ModelState.AddModelError(string.Empty, 
                    $"El alumno {member.User.FirstName} {member.User.LastName} posee plan Free (período de prueba). Las rutinas guiadas requieren suscripción a Plan Gold o Premium.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateAssignmentOptionsAsync(model, user, isAdminOrOwner, model.WorkoutPlanId, model.MemberProfileId);
            return View(model);
        }

        // Desactivar asignación previa de esta misma rutina si ya existía
        var previousActive = await _context.WorkoutAssignments
            .Where(a => a.MemberId == member!.Id && a.WorkoutPlanId == plan!.Id && a.IsActive)
            .ToListAsync();

        foreach (var prev in previousActive)
        {
            prev.IsActive = false;
        }

        // Crear nueva asignación
        var assignment = new WorkoutAssignment
        {
            MemberId = member!.Id,
            WorkoutPlanId = plan!.Id,
            AssignedByTrainerId = user.Id,
            AssignedAt = DateTime.UtcNow,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsActive = true
        };

        _context.WorkoutAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rutina ID {PlanId} ('{PlanName}') asignada al alumno {MemberName} por coach {Coach}.",
            plan.Id, plan.Name, $"{member.User.FirstName} {member.User.LastName}", user.Email);

        TempData["MensajeExito"] = $"Rutina '{plan.Name}' asignada exitosamente a {member.User.FirstName} {member.User.LastName} desde el {model.StartDate:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Details), new { id = plan.Id });
    }

    // ==========================================
    // 8. POST /WorkoutPlans/Unassign/{id}
    // Finalización controlada de una asignación activa
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner");

        var assignment = await _context.WorkoutAssignments
            .Include(a => a.Member).ThenInclude(m => m.User)
            .Include(a => a.WorkoutPlan)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            TempData["MensajeError"] = "La asignación no existe o ya fue finalizada.";
            return RedirectToAction(nameof(Index));
        }

        if (!isAdminOrOwner && assignment.AssignedByTrainerId != user.Id)
        {
            TempData["MensajeError"] = "No tienes permisos para modificar asignaciones realizadas por otro entrenador.";
            return RedirectToAction(nameof(Index));
        }

        assignment.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Asignación ID {Id} finalizada por {User}.", id, user.Email);
        TempData["MensajeExito"] = $"Se finalizó la asignación de la rutina '{assignment.WorkoutPlan.Name}' para el alumno {assignment.Member.User.FirstName} {assignment.Member.User.LastName}.";

        return RedirectToAction(nameof(Details), new { id = assignment.WorkoutPlanId });
    }

    // ==========================================
    // HELPERS AUXILIARES
    // ==========================================
    private async Task PopulateAssignmentOptionsAsync(
        WorkoutAssignmentCreateViewModel model, 
        ApplicationUser user, 
        bool isAdminOrOwner,
        int? specificPlanId = null,
        int? specificMemberId = null)
    {
        // 1. Cargar Rutinas Disponibles
        var plansQuery = _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Trainer)
            .Include(p => p.WorkoutExercises)
            .AsQueryable();

        if (!isAdminOrOwner)
        {
            // Entrenador: sus rutinas personalizadas + las rutinas generales del gimnasio
            plansQuery = plansQuery.Where(p => p.TrainerId == user.Id || p.IsGeneral);
        }

        var availablePlans = await plansQuery
            .OrderByDescending(p => p.IsGeneral)
            .ThenBy(p => p.Name)
            .Select(p => new WorkoutPlanOptionViewModel
            {
                Id = p.Id,
                Name = p.Name,
                IsGeneral = p.IsGeneral,
                ExercisesCount = p.WorkoutExercises.Count,
                TrainerName = p.Trainer != null ? $"{p.Trainer.FirstName} {p.Trainer.LastName}" : "Staff FitNet"
            })
            .ToListAsync();

        model.AvailablePlans = availablePlans;

        // Si hay rutina preseleccionada, recuperar metadatos
        if (specificPlanId.HasValue && specificPlanId.Value > 0)
        {
            var preselectedPlan = availablePlans.FirstOrDefault(p => p.Id == specificPlanId.Value);
            if (preselectedPlan != null)
            {
                model.PreselectedPlanName = preselectedPlan.Name;
                model.PreselectedPlanIsGeneral = preselectedPlan.IsGeneral;
            }
        }

        // 2. Cargar Alumnos Elegibles (Socios de Pago Gold y Premium)
        var membersQuery = _context.MemberProfiles
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.Membership)
            .Include(m => m.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.WorkoutPlan)
            .Where(m => m.Membership.Name != "Free") // Regla de Negocio: Excluir Trial
            .AsQueryable();

        var eligibleMembers = await membersQuery
            .OrderBy(m => m.User.LastName)
            .ThenBy(m => m.User.FirstName)
            .Select(m => new MemberAssignmentOptionViewModel
            {
                Id = m.Id,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email ?? string.Empty,
                PlanName = m.Membership.Name,
                AvatarPath = m.User.AvatarPath,
                CurrentWorkoutName = m.Assignments.Where(a => a.IsActive).Select(a => a.WorkoutPlan.Name).FirstOrDefault()
            })
            .ToListAsync();

        model.AvailableMembers = eligibleMembers;

        // Si hay alumno preseleccionado, recuperar metadatos
        if (specificMemberId.HasValue && specificMemberId.Value > 0)
        {
            var preselectedMember = eligibleMembers.FirstOrDefault(m => m.Id == specificMemberId.Value);
            if (preselectedMember != null)
            {
                model.PreselectedMemberName = preselectedMember.FullName;
                model.PreselectedMemberEmail = preselectedMember.Email;
                model.PreselectedMemberPlan = preselectedMember.PlanName;
                model.PreselectedMemberAvatar = preselectedMember.AvatarPath;
            }
        }
    }

    private async Task PopulateCreateAvailableMembersAsync(WorkoutPlanCreateViewModel model)
    {
        model.AvailableMembers = await _context.MemberProfiles
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.Membership)
            .Include(m => m.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.WorkoutPlan)
            .Where(m => m.User.IsActive && m.Membership.Name != "Free")
            .OrderBy(m => m.User.LastName)
            .ThenBy(m => m.User.FirstName)
            .Select(m => new MemberAssignmentOptionViewModel
            {
                Id = m.Id,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email ?? string.Empty,
                PlanName = m.Membership.Name,
                AvatarPath = m.User.AvatarPath,
                CurrentWorkoutName = m.Assignments.Where(a => a.IsActive).Select(a => a.WorkoutPlan.Name).FirstOrDefault()
            })
            .ToListAsync();
    }
}
