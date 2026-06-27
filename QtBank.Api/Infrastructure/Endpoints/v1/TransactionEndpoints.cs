using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Application.DTOs;
using FluentValidation;

namespace QtBank.Api.Infrastructure.Endpoints.v1;

/// <summary>
/// Exposes endpoints for managing bank transactions.
/// </summary>
public static class TransactionEndpoints
{
    /// <summary>
    /// Maps transaction endpoints to the request pipeline.
    /// </summary>
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/transactions/transfer", async (TransferRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new TransferCommand(
                    request.SourceAccountNumber,
                    request.DestinationAccountNumber,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );
                var result = await mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new { error = result.Error });
                }

                // Return 202 Accepted containing the TransactionId, Status, and Timestamp
                return Results.Accepted(uri: null, value: result.Value);
            }
            catch (ValidationException)
            {
                // This will be caught by the ValidationExceptionMiddleware to return 400 BadRequest with details.
                throw;
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while processing the transfer.");
            }
        })
        .RequireAuthorization()
        .WithName("TransferFunds")
        .Produces<TransferResponse>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Initiate a peer-to-peer (P2P) money transfer",
            Description = "Transfers money from a source account to a destination account. Validates active accounts, sufficient balance, and handles idempotency."
        })
        .WithTags("Transactions");

        app.MapPost("/api/v1/transactions/deposit", async (DepositRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new DepositCommand(
                    request.AccountNumber,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );
                var result = await mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new { error = result.Error });
                }

                // Return 202 Accepted containing the TransactionId, Status, and Timestamp
                return Results.Accepted(uri: null, value: result.Value);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while processing the deposit.");
            }
        })
        .RequireAuthorization()
        .WithName("DepositFunds")
        .Produces<TransferResponse>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Deposit money into a bank account",
            Description = "Deposits the specified amount into the destination account. Validates active account status."
        })
        .WithTags("Transactions");

        app.MapPost("/api/v1/transactions/withdrawal", async (WithdrawalRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new WithdrawalCommand(
                    request.AccountNumber,
                    request.Amount,
                    request.Currency,
                    request.IdempotencyKey
                );
                var result = await mediator.Send(command);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new { error = result.Error });
                }

                // Return 202 Accepted containing the TransactionId, Status, and Timestamp
                return Results.Accepted(uri: null, value: result.Value);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while processing the withdrawal.");
            }
        })
        .RequireAuthorization()
        .WithName("WithdrawFunds")
        .Produces<TransferResponse>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Withdraw money from a bank account",
            Description = "Withdraws the specified amount from the source account. Validates active account status and sufficient balance."
        })
        .WithTags("Transactions");

        return app;
    }
}
