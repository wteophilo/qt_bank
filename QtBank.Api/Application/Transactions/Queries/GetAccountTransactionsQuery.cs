using System.Collections.Generic;
using MediatR;
using QtBank.Api.Application.DTOs;

namespace QtBank.Api.Application.Transactions.Queries;

/// <summary>
/// Query to retrieve all transactions for a specific bank account.
/// </summary>
public record GetAccountTransactionsQuery(string AccountNumber) : IRequest<IEnumerable<TransactionResponse>?>;
