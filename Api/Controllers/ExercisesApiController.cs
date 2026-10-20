using System.Security.Claims;
using FitNet.Data;
using FitNet.DTOs.Common;
using FitNet.DTOs.Exercises;
using FitNet.Models;
using FitNet.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Api.Controllers;

[ApiController]
[Route("api/exercises")]
[Produces("application/json")]
public class ExercisesApiController : ControllerBase
{
    private const string AuthSchemes = "Identity.Application,Bearer";

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMembershipService _membershipService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExercisesApiController> _logger;

    public ExercisesApiController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IMembershipService membershipService,
        UserManager<ApplicationUser> userManager,
        ILogger<ExercisesApiController> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _membershipService = membershipService;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // 1. GET /api/exercises
    // Paginación 100% server-side con EF Core (Skip/Take), ordenamiento y filtros
    // ==========================================
    [HttpGet]
    [Authorize(AuthenticationSchemes = AuthSchemes)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<ExerciseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] ExerciseFilterDto filter)
    {
        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Owner") || User.IsInRole("Trainer");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        // Regla de Negocio: Si es socio (Member) y su trial expiró, no puede consultar
        if (!isPrivileged && !string.IsNullOrEmpty(userId))
        {
            var status = await _membershipService.GetMemberStatusAsync(userId);
            if (status.IsTrialExpired)
            {
                return StatusCode(StatusCodes.Status403Forbidden, 
                    ApiResponseDto.Fail("Tu período de prueba de 24 horas ha expirado. Suscribite a Plan Gold o Premium para acceder al catálogo."));
            }
        }

        var query = _context.Exercises
            .AsNoTracking()
            .Include(e => e.WorkoutExercises)
            .AsQueryable();

        // Seguridad: Los no privilegiados solo ven ejercicios activos
        if (!isPrivileged)
        {
            query = query.Where(e => e.IsActive);
        }
        else if (filter.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == filter.IsActive.Value);
        }

        // Filtro por texto (Nombre o Descripción)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term) || (e.Description != null && e.Description.ToLower().Contains(term)));
        }

        // Filtro por grupo muscular
        if (!string.IsNullOrWhiteSpace(filter.MuscleGroup))
        {
            var mg = filter.MuscleGroup.Trim();
            query = query.Where(e => e.MuscleGroup == mg);
        }

        // Filtro por dificultad
        if (!string.IsNullOrWhiteSpace(filter.Difficulty))
        {
            var diff = filter.Difficulty.Trim();
            query = query.Where(e => e.Difficulty == diff);
        }

        // Filtro por visibilidad pública / trial
        if (filter.IsPublic.HasValue)
        {
            query = query.Where(e => e.IsPublic == filter.IsPublic.Value);
        }

        // Ordenamiento dinámico en servidor
        var sortBy = filter.SortBy?.Trim().ToLowerInvariant() ?? "name";
        query = sortBy switch
        {
            "musclegroup" => filter.SortDesc ? query.OrderByDescending(e => e.MuscleGroup) : query.OrderBy(e => e.MuscleGroup),
            "difficulty" => filter.SortDesc ? query.OrderByDescending(e => e.Difficulty) : query.OrderBy(e => e.Difficulty),
            "createdat" => filter.SortDesc ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            _ => filter.SortDesc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name)
        };

        // 1. Total de registros que cumplen el filtro (Count en servidor)
        var totalItems = await query.CountAsync();

        // 2. Paginación server-side con Skip/Take
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                MuscleGroup = e.MuscleGroup,
                Difficulty = e.Difficulty,
                ImagePath = e.ImagePath,
                VideoUrl = e.VideoUrl,
                IsPublic = e.IsPublic,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                WorkoutsCount = e.WorkoutExercises.Count
            })
            .ToListAsync();

        var result = new PagedResultDto<ExerciseDto>(items, totalItems, filter.Page, filter.PageSize);
        return Ok(ApiResponseDto<PagedResultDto<ExerciseDto>>.Ok(result));
    }

    // ==========================================
    // 2. GET /api/exercises/{id}
    // Detalle de un ejercicio específico
    // ==========================================
    [HttpGet("{id:int}")]
    [Authorize(AuthenticationSchemes = AuthSchemes)]
    [ProducesResponseType(typeof(ApiResponseDto<ExerciseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Include(e => e.WorkoutExercises)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
        {
            return NotFound(ApiResponseDto.Fail($"No se encontró ningún ejercicio con el identificador {id}."));
        }

        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Owner") || User.IsInRole("Trainer");
        if (!isPrivileged && !exercise.IsActive)
        {
            return NotFound(ApiResponseDto.Fail($"El ejercicio seleccionado no se encuentra disponible."));
        }

        var dto = new ExerciseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            MuscleGroup = exercise.MuscleGroup,
            Difficulty = exercise.Difficulty,
            ImagePath = exercise.ImagePath,
            VideoUrl = exercise.VideoUrl,
            IsPublic = exercise.IsPublic,
            IsActive = exercise.IsActive,
            CreatedAt = exercise.CreatedAt,
            WorkoutsCount = exercise.WorkoutExercises.Count
        };

        return Ok(ApiResponseDto<ExerciseDto>.Ok(dto));
    }

    // ==========================================
    // 3. POST /api/exercises
    // Alta de ejercicio con soporte multipart para imagen técnica vía FileStorageService
    // ==========================================
    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes, Roles = "Admin,Owner")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<ExerciseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromForm] ExerciseCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto.Fail("Existen errores en los datos enviados.", GetModelStateErrors()));
        }

        var normalizedName = dto.Name.Trim();
        var exists = await _context.Exercises
            .AnyAsync(e => e.Name.ToLower() == normalizedName.ToLower());

        if (exists)
        {
            return Conflict(ApiResponseDto.Fail($"Ya existe un ejercicio registrado con el nombre '{normalizedName}'."));
        }

        // Subida y validación de imagen técnica opcional
        string? imagePath = null;
        if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            if (!_fileStorageService.ValidateImage(dto.ImageFile, out var errorMessage))
            {
                return BadRequest(ApiResponseDto.Fail(errorMessage ?? "El archivo de imagen no es válido."));
            }

            imagePath = await _fileStorageService.SaveFileAsync(dto.ImageFile, "exercises");
        }

        var exercise = new Exercise
        {
            Name = normalizedName,
            Description = dto.Description?.Trim(),
            MuscleGroup = dto.MuscleGroup.Trim(),
            Difficulty = dto.Difficulty.Trim(),
            VideoUrl = dto.VideoUrl?.Trim(),
            ImagePath = imagePath,
            IsPublic = dto.IsPublic,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ejercicio '{Name}' (ID: {Id}) creado exitosamente por usuario {User}.", 
            exercise.Name, exercise.Id, User.Identity?.Name);

        var responseDto = new ExerciseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            MuscleGroup = exercise.MuscleGroup,
            Difficulty = exercise.Difficulty,
            ImagePath = exercise.ImagePath,
            VideoUrl = exercise.VideoUrl,
            IsPublic = exercise.IsPublic,
            IsActive = exercise.IsActive,
            CreatedAt = exercise.CreatedAt,
            WorkoutsCount = 0
        };

        return CreatedAtAction(nameof(GetById), new { id = exercise.Id }, 
            ApiResponseDto<ExerciseDto>.Ok(responseDto, "Ejercicio creado exitosamente."));
    }

    // ==========================================
    // 4. PUT /api/exercises/{id}
    // Modificación completa y actualización / reemplazo de imagen técnica
    // ==========================================
    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = AuthSchemes, Roles = "Admin,Owner")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<ExerciseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromForm] ExerciseUpdateDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(ApiResponseDto.Fail("El identificador de la URL no coincide con el recurso enviado."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponseDto.Fail("Existen errores en los datos enviados.", GetModelStateErrors()));
        }

        var exercise = await _context.Exercises
            .Include(e => e.WorkoutExercises)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
        {
            return NotFound(ApiResponseDto.Fail($"No se encontró ningún ejercicio con el identificador {id}."));
        }

        var normalizedName = dto.Name.Trim();
        var duplicate = await _context.Exercises
            .AnyAsync(e => e.Id != id && e.Name.ToLower() == normalizedName.ToLower());

        if (duplicate)
        {
            return Conflict(ApiResponseDto.Fail($"Ya existe otro ejercicio registrado con el nombre '{normalizedName}'."));
        }

        // Gestión de imagen: Reemplazo o eliminación física de la imagen previa
        if (dto.RemoveCurrentImage && !string.IsNullOrEmpty(exercise.ImagePath))
        {
            await _fileStorageService.DeleteFileAsync(exercise.ImagePath);
            exercise.ImagePath = null;
        }
        else if (dto.ImageFile != null && dto.ImageFile.Length > 0)
        {
            if (!_fileStorageService.ValidateImage(dto.ImageFile, out var errorMessage))
            {
                return BadRequest(ApiResponseDto.Fail(errorMessage ?? "El archivo de imagen no es válido."));
            }

            if (!string.IsNullOrEmpty(exercise.ImagePath))
            {
                await _fileStorageService.DeleteFileAsync(exercise.ImagePath);
            }

            exercise.ImagePath = await _fileStorageService.SaveFileAsync(dto.ImageFile, "exercises");
        }

        exercise.Name = normalizedName;
        exercise.Description = dto.Description?.Trim();
        exercise.MuscleGroup = dto.MuscleGroup.Trim();
        exercise.Difficulty = dto.Difficulty.Trim();
        exercise.VideoUrl = dto.VideoUrl?.Trim();
        exercise.IsPublic = dto.IsPublic;
        exercise.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ejercicio ID {Id} ('{Name}') actualizado exitosamente por {User}.", 
            exercise.Id, exercise.Name, User.Identity?.Name);

        var responseDto = new ExerciseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            MuscleGroup = exercise.MuscleGroup,
            Difficulty = exercise.Difficulty,
            ImagePath = exercise.ImagePath,
            VideoUrl = exercise.VideoUrl,
            IsPublic = exercise.IsPublic,
            IsActive = exercise.IsActive,
            CreatedAt = exercise.CreatedAt,
            WorkoutsCount = exercise.WorkoutExercises.Count
        };

        return Ok(ApiResponseDto<ExerciseDto>.Ok(responseDto, "Ejercicio actualizado exitosamente."));
    }

    // ==========================================
    // 5. DELETE /api/exercises/{id}
    // Baja lógica o física controlada según relaciones activas
    // ==========================================
    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = AuthSchemes, Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(ApiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var exercise = await _context.Exercises
            .Include(e => e.WorkoutExercises)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
        {
            return NotFound(ApiResponseDto.Fail($"No se encontró ningún ejercicio con el identificador {id}."));
        }

        var hasActiveRelations = exercise.WorkoutExercises.Any();

        if (hasActiveRelations)
        {
            // Baja lógica controlada: preserva la integridad de las rutinas históricas
            exercise.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Baja lógica aplicada al ejercicio ID {Id} ('{Name}') por poseer {Count} rutinas vinculadas.", 
                exercise.Id, exercise.Name, exercise.WorkoutExercises.Count);

            return Ok(ApiResponseDto.Ok($"El ejercicio '{exercise.Name}' está prescripto en {exercise.WorkoutExercises.Count} rutina(s) activa(s). Se aplicó baja lógica (desactivación) para mantener la integridad histórica."));
        }
        else
        {
            // Baja física completa: borrado en base de datos y eliminación del archivo en disco
            if (!string.IsNullOrEmpty(exercise.ImagePath))
            {
                await _fileStorageService.DeleteFileAsync(exercise.ImagePath);
            }

            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Baja física aplicada al ejercicio ID {Id} ('{Name}') junto con sus archivos.", 
                exercise.Id, exercise.Name);

            return Ok(ApiResponseDto.Ok($"El ejercicio '{exercise.Name}' no posee rutinas vinculadas y fue eliminado físicamente del sistema junto con sus recursos multimedia."));
        }
    }

    // ==========================================
    // 6. GET /api/exercises/search?q=
    // Búsqueda rápida optimizada (máx. 10 registros) para selects dinámicos y autocompletados
    // ==========================================
    [HttpGet("search")]
    [Authorize(AuthenticationSchemes = AuthSchemes)]
    [ProducesResponseType(typeof(ApiResponseDto<List<ExerciseSearchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        var query = _context.Exercises
            .AsNoTracking()
            .Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term) || e.MuscleGroup.ToLower().Contains(term));
        }

        var results = await query
            .OrderBy(e => e.Name)
            .Take(10)
            .Select(e => new ExerciseSearchDto
            {
                Id = e.Id,
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                Difficulty = e.Difficulty,
                ImagePath = e.ImagePath,
                IsPublic = e.IsPublic
            })
            .ToListAsync();

        return Ok(ApiResponseDto<List<ExerciseSearchDto>>.Ok(results));
    }

    // ==========================================
    // HELPERS PRIVADOS
    // ==========================================
    private IDictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }
}
