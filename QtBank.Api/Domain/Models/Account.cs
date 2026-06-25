using System;

namespace QtBank.Api.Domain.Models;

public class Account
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
}
