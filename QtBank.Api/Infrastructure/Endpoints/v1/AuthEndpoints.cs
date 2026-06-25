using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QtBank.Api.Infrastructure.Security;

namespace QtBank.Api.Infrastructure.Endpoints.v1;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Auth Endpoint (Public)
        app.MapPost("/auth/token", (TokenRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest("Username is required.");
            }

            var token = TokenGenerator.GenerateToken(request.Username);
            return Results.Ok(new TokenResponse(token));
        })
        .WithName("GenerateToken")
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Generate authentication JWT token",
            Description = "Generates a secure JSON Web Token (JWT) for the specified username. This token is required to access all protected API endpoints."
        })
        .WithTags("Authentication");

        return app;
    }
}

/// <summary>
/// Request payload for generating a security token.
/// </summary>
/// <param name="Username">The username of the user requesting authorization.</param>
public record TokenRequest(string Username);

/// <summary>
/// Response payload containing the generated bearer JWT token.
/// </summary>
/// <param name="Token">The secure JWT token string.</param>
public record TokenResponse(string Token);
