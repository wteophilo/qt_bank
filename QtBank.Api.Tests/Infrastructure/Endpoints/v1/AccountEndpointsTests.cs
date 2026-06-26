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
using QtBank.Api.Application.Accounts.Commands;
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
    public async Task CreateAccount_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new CreateAccountCommand("123456", 1000m, "Alice Smith", AccountStatus.Active);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_WithValidCommand_Returns201Created_AndReturnsAccountDto()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new CreateAccountCommand("987654", 2500m, "Jane Doe", AccountStatus.Active);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<AccountDto>();
        dto.Should().NotBeNull();
        dto!.AccountNumber.Should().Be("987654");
        dto.OwnerName.Should().Be("Jane Doe");
        dto.Balance.Should().Be(2500m);
        dto.Status.Should().Be("Active");
        dto.Id.Should().NotBeEmpty();

        response.Headers.Location!.ToString().Should().Be($"/api/v1/accounts/{dto.Id}");
    }

    [Fact]
    public async Task CreateAccount_WithInvalidCommand_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Missing account number and owner name, negative balance
        var command = new CreateAccountCommand("", -10m, "", AccountStatus.Active);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", command);

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

        // Extract validation errors extension
        problemDetails.Extensions.Should().ContainKey("errors");
        var errorsJson = problemDetails.Extensions["errors"]?.ToString();
        errorsJson.Should().NotBeNull();

        var errors = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(errorsJson!);
        errors.Should().NotBeNull();
        errors.Should().ContainKey("AccountNumber");
        errors.Should().ContainKey("OwnerName");
        errors.Should().ContainKey("Balance");
    }

    [Fact]
    public async Task CreateAccount_WhenMediatorThrowsArgumentException_Returns400BadRequest_WithErrorMessage()
    {
        // Arrange
        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateAccountCommand>(), Arg.Any<CancellationToken>())
            .Throws(new ArgumentException("Invalid business argument."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockMediator);
            });
        }).CreateClient();

        var token = TokenGenerator.GenerateToken("test-user");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateAccountCommand("111222", 100m, "Bob Smith", AccountStatus.Active);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Invalid business argument.");
    }

    [Fact]
    public async Task CreateAccount_WhenMediatorThrowsUnexpectedException_Returns500InternalServerError()
    {
        // Arrange
        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateAccountCommand>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(mockMediator);
            });
        }).CreateClient();

        var token = TokenGenerator.GenerateToken("test-user");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new CreateAccountCommand("111222", 100m, "Bob Smith", AccountStatus.Active);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("An error occurred while creating the account.");
        problemDetails.Detail.Should().Be("Database connection failed.");
        problemDetails.Status.Should().Be(500);
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

