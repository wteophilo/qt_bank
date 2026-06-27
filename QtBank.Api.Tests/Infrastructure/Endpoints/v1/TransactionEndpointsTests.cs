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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

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
        var aliceBeforeResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBefore = await aliceBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var aliceInitial = aliceBefore!.Balance;

        var bobBeforeResponse = await client.GetAsync("/api/v1/accounts/222222/balance");
        var bobBefore = await bobBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var bobInitial = bobBefore!.Balance;

        // Act - Execute Transfer
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

        // Assert Endpoint Response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var transferResult = await response.Content.ReadFromJsonAsync<TransferResponse>();
        transferResult.Should().NotBeNull();
        transferResult!.TransactionId.Should().NotBeEmpty();
        transferResult.Status.Should().Be("Processing");
        transferResult.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Act - Verify Balances Updated (Alice: -100m; Bob: +100m)
        var aliceBalanceResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBalance = await aliceBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        aliceBalance.Should().NotBeNull();
        aliceBalance!.Balance.Should().Be(aliceInitial - 100m);

        var bobBalanceResponse = await client.GetAsync("/api/v1/accounts/222222/balance");
        var bobBalance = await bobBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", rawPayload);

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
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("An error occurred while processing the transfer.");
        problemDetails.Detail.Should().Be("Database connection failed");
        problemDetails.Status.Should().Be(500);
    }

    [Fact]
    public async Task Deposit_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new DepositCommand("111111", 100m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deposit_WithValidPayload_Returns202Accepted_AndUpdatesBalance()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new DepositCommand("111111", 200m, Currency.USD);

        // Get initial balance
        var aliceBeforeResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBefore = await aliceBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var aliceInitial = aliceBefore!.Balance;

        // Act - Execute Deposit
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

        // Assert Endpoint Response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var depositResult = await response.Content.ReadFromJsonAsync<TransferResponse>();
        depositResult.Should().NotBeNull();
        depositResult!.TransactionId.Should().NotBeEmpty();
        depositResult.Status.Should().Be("Processing");
        depositResult.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Act - Verify Balance Updated (Alice: +200m)
        var aliceBalanceResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBalance = await aliceBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        aliceBalance.Should().NotBeNull();
        aliceBalance!.Balance.Should().Be(aliceInitial + 200m);
    }

    [Fact]
    public async Task Deposit_WithInvalidPayload_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Negative amount, invalid currency
        var command = new DepositCommand("111111", -50m, (Currency)999);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

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
        errors.Should().ContainKey("Amount");
        errors.Should().ContainKey("Currency");
    }

    [Fact]
    public async Task Deposit_WhenAccountNotFound_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new DepositCommand("999999", 50m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Account not found.");
    }

    [Fact]
    public async Task Deposit_WhenAccountInactive_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new DepositCommand("333333", 10m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Account is not active.");
    }

    [Fact]
    public async Task Deposit_WithInvalidCurrencyString_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            AccountNumber = "111111",
            Amount = 100m,
            Currency = "KHR" // Invalid string for Currency enum
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", rawPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_WhenExceptionOccurs_Returns500InternalServerError()
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

        var command = new DepositCommand("111111", 100m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("An error occurred while processing the deposit.");
        problemDetails.Detail.Should().Be("Database connection failed");
        problemDetails.Status.Should().Be(500);
    }

    [Fact]
    public async Task Withdrawal_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new WithdrawalCommand("111111", 100m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Withdrawal_WithValidPayload_Returns202Accepted_AndUpdatesBalance()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new WithdrawalCommand("111111", 200m, Currency.USD);

        // Get initial balance
        var aliceBeforeResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBefore = await aliceBeforeResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var aliceInitial = aliceBefore!.Balance;

        // Act - Execute Withdrawal
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert Endpoint Response
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var withdrawalResult = await response.Content.ReadFromJsonAsync<TransferResponse>();
        withdrawalResult.Should().NotBeNull();
        withdrawalResult!.TransactionId.Should().NotBeEmpty();
        withdrawalResult.Status.Should().Be("Processing");
        withdrawalResult.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Act - Verify Balance Updated (Alice: -200m)
        var aliceBalanceResponse = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBalance = await aliceBalanceResponse.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        aliceBalance.Should().NotBeNull();
        aliceBalance!.Balance.Should().Be(aliceInitial - 200m);
    }

    [Fact]
    public async Task Withdrawal_WithInvalidPayload_Returns400BadRequest_WithValidationErrors()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Negative amount, invalid currency
        var command = new WithdrawalCommand("111111", -50m, (Currency)999);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

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
        errors.Should().ContainKey("Amount");
        errors.Should().ContainKey("Currency");
    }

    [Fact]
    public async Task Withdrawal_WhenAccountNotFound_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new WithdrawalCommand("999999", 50m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Account not found.");
    }

    [Fact]
    public async Task Withdrawal_WhenAccountInactive_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var command = new WithdrawalCommand("333333", 10m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Account is not active.");
    }

    [Fact]
    public async Task Withdrawal_WhenInsufficientFunds_Returns400BadRequest_WithBusinessError()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        // Alice has enough to debit 10000m? No, she has 5000m initial
        var command = new WithdrawalCommand("111111", 10000m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        errorResponse.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        errorProperty.GetString().Should().Be("Insufficient funds.");
    }

    [Fact]
    public async Task Withdrawal_WithInvalidCurrencyString_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            AccountNumber = "111111",
            Amount = 100m,
            Currency = "KHR" // Invalid string for Currency enum
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", rawPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdrawal_WhenExceptionOccurs_Returns500InternalServerError()
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

        var command = new WithdrawalCommand("111111", 100m, Currency.USD);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("An error occurred while processing the withdrawal.");
        problemDetails.Detail.Should().Be("Database connection failed");
        problemDetails.Status.Should().Be(500);
    }

    [Fact]
    public async Task Transfer_WithMissingIdempotencyKey_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            Amount = 100m,
            Currency = "USD"
            // IdempotencyKey is missing
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/transfer", rawPayload);

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
        errors.Should().ContainKey("IdempotencyKey");
        errors["IdempotencyKey"].Should().Contain("Idempotency key is required.");
    }

    [Fact]
    public async Task Deposit_WithMissingIdempotencyKey_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            AccountNumber = "111111",
            Amount = 100m,
            Currency = "USD"
            // IdempotencyKey is missing
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/deposit", rawPayload);

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
        errors.Should().ContainKey("IdempotencyKey");
        errors["IdempotencyKey"].Should().Contain("Idempotency key is required.");
    }

    [Fact]
    public async Task Withdrawal_WithMissingIdempotencyKey_Returns400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var rawPayload = new
        {
            AccountNumber = "111111",
            Amount = 100m,
            Currency = "USD"
            // IdempotencyKey is missing
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", rawPayload);

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
        errors.Should().ContainKey("IdempotencyKey");
        errors["IdempotencyKey"].Should().Contain("Idempotency key is required.");
    }

    [Fact]
    public async Task Deposit_IsIdempotent_DoesNotMutateBalanceTwice()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var idempotencyKey = Guid.NewGuid();
        var command = new DepositCommand("111111", 150m, Currency.USD, idempotencyKey);

        // Get initial balance
        var balanceResponseBefore = await client.GetAsync("/api/v1/accounts/111111/balance");
        var beforeDto = await balanceResponseBefore.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var initialBalance = beforeDto!.Balance;

        // Act - First request
        var firstResponse = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<TransferResponse>();
        firstResult.Should().NotBeNull();

        // Get balance after first deposit
        var balanceResponseMiddle = await client.GetAsync("/api/v1/accounts/111111/balance");
        var middleDto = await balanceResponseMiddle.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        middleDto!.Balance.Should().Be(initialBalance + 150m);

        // Act - Second request with the same idempotency key
        var secondResponse = await client.PostAsJsonAsync("/api/v1/transactions/deposit", command);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<TransferResponse>();
        secondResult.Should().NotBeNull();

        // Assert that the returned transaction ID and status are the same as the first request
        secondResult!.TransactionId.Should().Be(firstResult!.TransactionId);

        // Verify balance did not change again (remains initial + 150m)
        var balanceResponseAfter = await client.GetAsync("/api/v1/accounts/111111/balance");
        var afterDto = await balanceResponseAfter.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        afterDto!.Balance.Should().Be(initialBalance + 150m);
    }

    [Fact]
    public async Task Withdrawal_IsIdempotent_DoesNotMutateBalanceTwice()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var idempotencyKey = Guid.NewGuid();
        var command = new WithdrawalCommand("111111", 150m, Currency.USD, idempotencyKey);

        // Get initial balance
        var balanceResponseBefore = await client.GetAsync("/api/v1/accounts/111111/balance");
        var beforeDto = await balanceResponseBefore.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var initialBalance = beforeDto!.Balance;

        // Act - First request
        var firstResponse = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<TransferResponse>();
        firstResult.Should().NotBeNull();

        // Get balance after first withdrawal
        var balanceResponseMiddle = await client.GetAsync("/api/v1/accounts/111111/balance");
        var middleDto = await balanceResponseMiddle.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        middleDto!.Balance.Should().Be(initialBalance - 150m);

        // Act - Second request with the same idempotency key
        var secondResponse = await client.PostAsJsonAsync("/api/v1/transactions/withdrawal", command);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<TransferResponse>();
        secondResult.Should().NotBeNull();

        // Assert that the returned transaction ID and status are the same as the first request
        secondResult!.TransactionId.Should().Be(firstResult!.TransactionId);

        // Verify balance did not change again (remains initial - 150m)
        var balanceResponseAfter = await client.GetAsync("/api/v1/accounts/111111/balance");
        var afterDto = await balanceResponseAfter.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        afterDto!.Balance.Should().Be(initialBalance - 150m);
    }

    [Fact]
    public async Task Transfer_IsIdempotent_DoesNotMutateBalanceTwice()
    {
        // Arrange
        var client = CreateAuthorizedClient();
        var idempotencyKey = Guid.NewGuid();
        var command = new TransferCommand("111111", "222222", 150m, Currency.USD, idempotencyKey);

        // Get initial balances
        var aliceResponseBefore = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceBefore = await aliceResponseBefore.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var aliceInitial = aliceBefore!.Balance;

        var bobResponseBefore = await client.GetAsync("/api/v1/accounts/222222/balance");
        var bobBefore = await bobResponseBefore.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        var bobInitial = bobBefore!.Balance;

        // Act - First request
        var firstResponse = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<TransferResponse>();
        firstResult.Should().NotBeNull();

        // Get middle balances
        var aliceResponseMiddle = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceMiddle = await aliceResponseMiddle.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        aliceMiddle!.Balance.Should().Be(aliceInitial - 150m);

        var bobResponseMiddle = await client.GetAsync("/api/v1/accounts/222222/balance");
        var bobMiddle = await bobResponseMiddle.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        bobMiddle!.Balance.Should().Be(bobInitial + 150m);

        // Act - Second request with the same idempotency key
        var secondResponse = await client.PostAsJsonAsync("/api/v1/transactions/transfer", command);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<TransferResponse>();
        secondResult.Should().NotBeNull();

        // Assert that the returned transaction ID and status are the same as the first request
        secondResult!.TransactionId.Should().Be(firstResult!.TransactionId);

        // Verify balances did not change again
        var aliceResponseAfter = await client.GetAsync("/api/v1/accounts/111111/balance");
        var aliceAfter = await aliceResponseAfter.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        aliceAfter!.Balance.Should().Be(aliceInitial - 150m);

        var bobResponseAfter = await client.GetAsync("/api/v1/accounts/222222/balance");
        var bobAfter = await bobResponseAfter.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        bobAfter!.Balance.Should().Be(bobInitial + 150m);
    }
}


