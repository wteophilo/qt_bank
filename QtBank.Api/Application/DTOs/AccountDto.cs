using System;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Application.DTOs;

public record AccountDto(
    Guid Id,
    string AccountNumber,
    decimal Balance,
    string OwnerName,
    DateTime CreatedAt,
    string Status
);
