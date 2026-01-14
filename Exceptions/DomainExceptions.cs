namespace erp.Exceptions;

/// <summary>
/// Base exception for domain-specific errors in the Pillar ERP application.
/// </summary>
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key)
        : base("NOT_FOUND", $"{resourceName} with ID '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    public NotFoundException(string message)
        : base("NOT_FOUND", message)
    {
    }

    public string? ResourceName { get; }
    public object? Key { get; }
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message)
        : base("BUSINESS_RULE_VIOLATION", message)
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base("BUSINESS_RULE_VIOLATION", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_FAILED", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string fieldName, string errorMessage)
        : base("VALIDATION_FAILED", $"{fieldName}: {errorMessage}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = new[] { errorMessage }
        };
    }

    public ValidationException(string message)
        : base("VALIDATION_FAILED", message)
    {
        Errors = new Dictionary<string, string[]>();
    }
}

/// <summary>
/// Exception thrown when a user is not authorized to perform an action.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message)
        : base("UNAUTHORIZED", message)
    {
    }

    public UnauthorizedException(string resource, string action)
        : base("UNAUTHORIZED", $"You are not authorized to {action} this {resource}.")
    {
        Resource = resource;
        Action = action;
    }

    public string? Resource { get; }
    public string? Action { get; }
}

/// <summary>
/// Exception thrown when an operation conflicts with current state.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base("CONFLICT", message)
    {
    }

    public ConflictException(string resource, string reason)
        : base("CONFLICT", $"Cannot {resource}. {reason}")
    {
        Resource = resource;
        Reason = reason;
    }

    public string? Resource { get; }
    public string? Reason { get; }
}
