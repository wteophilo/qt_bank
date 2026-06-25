using QtBank.Api.Domain.Models;

namespace QtBank.Api.Domain.Events;

public record AccountCreated(
    Guid AccountId,
    string AccountNumber,
    decimal Balance,
    string OwnerName,
    DateTime CreatedAt,
    AccountStatus Status
);
