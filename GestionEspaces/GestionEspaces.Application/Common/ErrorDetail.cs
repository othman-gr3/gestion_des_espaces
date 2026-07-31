namespace GestionEspaces.Application.Common;

/// <summary>
/// Describes a business, validation, or infrastructure error.
/// </summary>
public sealed record ErrorDetail(string Code, string Message, string? PropertyName = null);