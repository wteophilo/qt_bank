using System;
using QtBank.Api.Domain.Exceptions;

namespace QtBank.Api.Domain.Models;

public class Account
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public bool CanDebit(decimal amount) => Status == AccountStatus.Active && Balance >= amount;
    public bool IsActive() => Status == AccountStatus.Active;

    public void Debit(decimal amount)
    {
        if (!CanDebit(amount)) throw new DomainException("Cannot debit account.");
        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        if (!IsActive()) throw new DomainException("Cannot credit inactive account.");
        Balance += amount;
    }
}
