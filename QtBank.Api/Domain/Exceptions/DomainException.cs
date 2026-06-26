using System;

namespace QtBank.Api.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain business rule is violated.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
