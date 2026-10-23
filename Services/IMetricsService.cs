using FitNet.ViewModels.Admin;

namespace FitNet.Services;

/// <summary>
/// Contrato de servicio para la extracción y consolidación de métricas de negocio para el rol Owner.
/// Todas las consultas deben resolverse mediante agregaciones nativas en el motor de persistencia (MySQL).
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Computa las métricas financieras, comerciales y operativas del gimnasio
    /// utilizando consultas delegadas directamente en el servidor (CountAsync, SumAsync, GroupBy).
    /// </summary>
    Task<BusinessMetricsViewModel> GetBusinessMetricsAsync();
}
