namespace GestionEspaces.Application.Common;

/// <summary>
/// Represents a paged response.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);