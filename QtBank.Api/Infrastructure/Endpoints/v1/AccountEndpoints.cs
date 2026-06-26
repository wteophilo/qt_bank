using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QtBank.Api.Application.Accounts.Commands;
using QtBank.Api.Application.Accounts.Queries;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Security;
using FluentValidation;

namespace QtBank.Api.Infrastructure.Endpoints.v1;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Account Endpoints (Authorized CRUD)
        app.MapPost("/api/v1/accounts", async (CreateAccountCommand command, IMediator mediator) =>
        {
            try
            {
                var response = await mediator.Send(command);
                return Results.Created($"/api/v1/accounts/{response.Id}", response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "An error occurred while creating the account.");
            }
        })
        .RequireAuthorization()
        .WithName("CreateAccount")
        .Produces<AccountDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Create a new bank account",
            Description = "Creates a new bank account with the specified account number, initial balance, owner name, and status, and publishes an AccountCreated integration event."
        })
        .WithTags("Accounts");

        app.MapGet("/api/v1/accounts/{accountNumber}/balance", async (string accountNumber, IMediator mediator) =>
        {
            var response = await mediator.Send(new GetAccountBalanceQuery(accountNumber));
            if (response == null)
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

        return app;
    }
}