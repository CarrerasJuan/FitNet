namespace FitNet.Services;

/// <summary>
/// Representa el estado consolidado de suscripción, trial y permisos de acceso del usuario.
/// </summary>
public class MemberAccessStatus
{
    public string PlanName { get; set; } = "Free";
    public bool IsActive { get; set; }
    public bool IsTrial { get; set; }
    public bool IsTrialExpired { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int RemainingHours { get; set; }
    public int RemainingDays { get; set; }

    // Matriz de permisos del dominio
    public bool CanAccessPublicExercises { get; set; }
    public bool CanAccessFullLibrary { get; set; }
    public bool CanAccessWorkouts { get; set; }
    public bool CanAccessProgress { get; set; }

    // Representación visual
    public string StatusBadge { get; set; } = "Free";
    public string StatusMessage { get; set; } = string.Empty;
}

public interface IMembershipService
{
    /// <summary>
    /// Obtiene el estado consolidado de membresía, vigencia del trial y permisos del usuario.
    /// </summary>
    Task<MemberAccessStatus> GetMemberStatusAsync(string userId);

    /// <summary>
    /// Determina si el usuario tiene acceso a los ejercicios públicos (Free en trial, Gold o Premium).
    /// </summary>
    Task<bool> CanAccessPublicExercisesAsync(string userId);

    /// <summary>
    /// Determina si el usuario tiene acceso al catálogo completo de ejercicios (Gold, Premium, Staff).
    /// </summary>
    Task<bool> CanAccessFullLibraryAsync(string userId);

    /// <summary>
    /// Determina si el usuario puede acceder al módulo de rutinas (Gold, Premium, Staff).
    /// </summary>
    Task<bool> CanAccessWorkoutsAsync(string userId);

    /// <summary>
    /// Determina si el usuario puede acceder al registro y fotos de progreso (Gold, Premium, Staff).
    /// </summary>
    Task<bool> CanAccessProgressAsync(string userId);

    /// <summary>
    /// Valida si el período de prueba gratuita de 24 horas ha expirado.
    /// </summary>
    Task<bool> IsTrialExpiredAsync(string userId);

    /// <summary>
    /// Cambia o renueva la membresía de un socio (utilizado en checkout y administración).
    /// </summary>
    Task<bool> ChangeMembershipAsync(string userId, string planName, int durationMonths = 1);
}
