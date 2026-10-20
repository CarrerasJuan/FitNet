namespace FitNet.DTOs.Common;

/// <summary>
/// Contenedor genérico para respuestas paginadas del lado servidor.
/// </summary>
/// <typeparam name="T">Tipo del elemento contenido en el conjunto paginado.</typeparam>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResultDto()
    {
    }

    public PagedResultDto(IReadOnlyList<T> items, int totalItems, int page, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        Page = page;
        PageSize = pageSize;
    }
}
