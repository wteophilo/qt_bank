using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using QtBank.Api.Application.DTOs;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using QtBank.Api.Application.Transactions.Commands;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Security;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Endpoints.v1;

[Collection("Sequential")]
public class TransactionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionEndpointsTests(WebApplicationFactory<Program> factory)
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
    public async Task Transfer_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new TransferCommand(
            "111111",
            "222222",
            100m,
            Currency.USD
        );

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Transfer_WithValidPayload_Returns202Accepted_AndUpdatesBalances()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new TransferCommand("111111", "222222", 100m, Currency.USD);

        // Get initial balances
        var aliceBeforeResponse = await client.GetAsync("/accounts/111111/balance");
        var aliceBefore = await aliceBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceDto>();
        var aliceInitial = aliceBefore!.Balance;

        var bobBeforeResponse = await client.GetAsync("/accounts/222222/balance");
        var bobBefore = await bobBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceDto>();
        var bobInitial = bobBefore!.Balance;

        // Act - Execute Transfer
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert Endpoint Response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var transferResult = await response.Content.ReadFromJsonAsync<TransferResponseDto>();
        transferResult.Should().NotBeNull();
        transferResult!.TransactionId.Should().NotBeEmpty();
        transferResult.Status.Should().Be("Processing");
        transferResult.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Act - Verify Balances Updated (Alice: -100m; Bob: +100m)
        var aliceBalanceResponse = await client.GetAsync("/accounts/111111/balance");
        var aliceBalance = await aliceBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceDto>();
        aliceBalance.Should().NotBeNull();
        aliceBalance!.Balance.Should().Be(aliceInitial - 100m);

        var bobBalanceResponse = await client.GetAsync("/accounts/222222/balance");
        var bobBalance = await bobBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceDto>();
        bobBalance.Should().NotBeNull();
        bobBalance!.Balance.Should().Be(bobInitial + 100m);
    }

    [Fact]
    public async Task Transfer_WithInvalidPayload_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Negative amount, same source/destination, invalid currency
        var command = new TransferCommand("111111", "111111", -50m, (Currency)999);

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

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
        errors.Should().ContainKey("DestinationAccountNumber");
        errors.Should().ContainKey("Amount");
        errors.Should().ContainKey("Currency");
    }

    [Fact]
    public async Task Transfer_WhenSourceAccountNotFound_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new TransferCommand("999999", "222222", 50m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Source account not found.");
    }

    [Fact]
    public async Task Transfer_WhenInsufficientFunds_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Bob's initial balance is 150.50m (or around that depending on other test runs, but he definitely has less than 10000m)
        var command = new TransferCommand("222222", "111111", 10000m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Contain("Insufficient funds");
    }

    [Fact]
    public async Task Transfer_WhenSourceAccountInactive_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new TransferCommand("333333", "111111", 10m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Source account is not active.");
    }

    [Fact]
    public async Task Transfer_WithInvalidCurrencyString_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            Amount = 100m,
            Currency = "KHR" // Invalid string for Currency enum
        };

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", rawPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_WhenExceptionOccurs_Returns500InternalServerError()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockRepo = Substitute.For<IAccountRepository>();
                mockRepo.GetByNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<Account?>(new Exception("Database connection failed")));

                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAccountRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(mockRepo);
            });
        }).CreateClient();

        var token = TokenGenerator.GenerateToken("test-user");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var command = new TransferCommand("111111", "222222", 100m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("An error occurred while processing the transfer.");
        problemDetails.Detail.Should().Be("Database connection failed");
        problemDetails.Status.Should().Be(500);
    }
}
