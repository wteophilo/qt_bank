using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Collections.Generic;
using System.Linq;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Application.Transactions.Queries;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Security;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Endpoints.v1;

[Collection("Sequential")]
public class AccountEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthorizedClient(string username = "test-user")
    {
        var client = _factory.CreateClient();
        var token = TokenGenerator.GenerateToken(username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetAccountBalance_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/accounts/111111/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccountBalance_WithValidAccountNumber_Returns200OK_AndReturnsAccountBalanceDto()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "111111";

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountNumber}/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AccountBalanceDto>();
        dto.Should().NotBeNull();
        dto!.AccountNumber.Should().Be(accountNumber);
        dto.Balance.Should().Be(5000.00m);
    }

    [Fact]
    public async Task GetAccountBalance_WithNonExistentAccountNumber_Returns404NotFound()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "999999";

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountNumber}/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be($"Account with number '{accountNumber}' not found.");
    }

    [Fact]
    public async Task GetAccountBalance_WithInvalidAccountNumber_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "   "; // Whitespace

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{Uri.EscapeDataString(accountNumber)}/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var contentString = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(contentString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Status.Should().Be(400);

        problemDetails.Extensions.Should().ContainKey("errors");
        var errorsJson = problemDetails.Extensions["errors"]?.ToString();
        errorsJson.Should().NotBeNull();

        var errors = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(errorsJson!);
        errors.Should().NotBeNull();
        errors.Should().ContainKey("AccountNumber");
    }

    [Fact]
    public async Task GetTransactions_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/accounts/111111/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_WithValidAccountNumber_Returns200OK_AndReturnsTransactionsList()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "111111";

        // Let's execute a transfer first to ensure there's at least one transaction
        var command = new TransferCommand(accountNumber, "222222", 50m, Currency.USD);
        await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountNumber}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<TransactionDto>>();
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();
        list!.Any(t => t.SourceAccountNumber == accountNumber || t.DestinationAccountNumber == accountNumber).Should().BeTrue();
        
        var tx = list!.First(t => t.SourceAccountNumber == accountNumber);
        tx.Type.Should().Be("Transfer");
    }

    [Fact]
    public async Task GetTransactions_WithNonExistentAccountNumber_Returns404NotFound()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "999999";

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{accountNumber}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be($"Account with number '{accountNumber}' not found.");
    }

    [Fact]
    public async Task GetTransactions_WithInvalidAccountNumber_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var accountNumber = "   "; // Whitespace

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{Uri.EscapeDataString(accountNumber)}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var contentString = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(contentString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Status.Should().Be(400);

        problemDetails.Extensions.Should().ContainKey("errors");
        var errorsJson = problemDetails.Extensions["errors"]?.ToString();
        errorsJson.Should().NotBeNull();

        var errors = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(errorsJson!);
        errors.Should().NotBeNull();
        errors.Should().ContainKey("AccountNumber");
    }
}

