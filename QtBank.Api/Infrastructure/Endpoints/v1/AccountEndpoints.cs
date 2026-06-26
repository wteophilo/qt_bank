using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QtBank.Api.Application.Accounts.Queries;
using QtBank.Api.Application.Transactions.Queries;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Security;
using FluentValidation;

namespace QtBank.Api.Infrastructure.Endpoints.v1;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/accounts/{accountNumber}/balance", async (string accountNumber, IMediator mediator) =>
        {
            var response = await mediator.Send(new GetAccountBalanceQuery(accountNumber));
            if (response is null)
            {
                return Results.NotFound(new { error = $"Account with number '{accountNumber}' not found." });
            }
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("GetAccountBalance")
        .Produces<AccountBalanceDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get the balance of a bank account",
            Description = "Retrieves the balance of the bank account corresponding to the specified account number."
        })
        .WithTags("Accounts");

        app.MapGet("/api/v1/accounts/{accountNumber}/transactions", async (string accountNumber, IMediator mediator) =>
        {
            try
            {
                var response = await mediator.Send(new GetAccountTransactionsQuery(accountNumber));
                if (response is null)
                {
                    return Results.NotFound(new { error = $"Account with number '{accountNumber}' not found." });
                }
                return Results.Ok(response);
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while retrieving transactions.");
            }
        })
        .RequireAuthorization()
        .WithName("GetAccountTransactions")
        .Produces<IEnumerable<TransactionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get transactions of a bank account",
            Description = "Retrieves all transactions where the specified account number was the source or the destination."
        })
        .WithTags("Accounts");

        return app;
    }
}