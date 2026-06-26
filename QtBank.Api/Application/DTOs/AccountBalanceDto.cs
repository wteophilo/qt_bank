using System;

namespace QtBank.Api.Application.DTOs;

public record AccountBalanceDto(
    string AccountNumber,
    decimal Balance
);
