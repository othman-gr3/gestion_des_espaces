namespace GestionEspaces.Domain.Exceptions;

/// <summary>
/// Raised when a business rule is violated.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }
}