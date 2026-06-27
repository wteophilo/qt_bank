using System;

namespace QtBank.Api.Application.DTOs;

public record AccountBalanceResponse(
    string AccountNumber,
    decimal Balance
);
