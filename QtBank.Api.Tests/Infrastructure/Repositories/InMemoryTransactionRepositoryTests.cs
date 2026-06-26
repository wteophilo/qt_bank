using System;
using System.Threading.Tasks;
using FluentAssertions;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Repositories;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Repositories;

public class InMemoryTransactionRepositoryTests
{
    private readonly InMemoryTransactionRepository _repository;

    public InMemoryTransactionRepositoryTests()
    {
        _repository = new InMemoryTransactionRepository();
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreTransaction_AndGetByIdempotencyKeyAsyncShouldRetrieveIt()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            Amount = 150.00m,
            Currency = Currency.USD,
            Type = TransactionType.Deposit,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var savedTx = await _repository.SaveAsync(transaction);
        var retrievedTx = await _repository.GetByIdempotencyKeyAsync(transaction.IdempotencyKey);

        // Assert
        savedTx.Should().BeEquivalentTo(transaction);
        retrievedTx.Should().NotBeNull();
        retrievedTx!.Should().BeEquivalentTo(transaction);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ShouldReturnNull_WhenTransactionDoesNotExist()
    {
        // Arrange
        var nonExistentKey = Guid.NewGuid();

        // Act
        var retrievedTx = await _repository.GetByIdempotencyKeyAsync(nonExistentKey);

        // Assert
        retrievedTx.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldUpdateTransaction_WhenTransactionAlreadyExists()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            Amount = 150.00m,
            Currency = Currency.USD,
            Type = TransactionType.Withdrawal,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(transaction);

        // Modify the status
        transaction.Status = TransactionStatus.Completed;

        // Act
        var updatedTx = await _repository.SaveAsync(transaction);
        var retrievedTx = await _repository.GetByIdempotencyKeyAsync(transaction.IdempotencyKey);

        // Assert
        updatedTx.Status.Should().Be(TransactionStatus.Completed);
        retrievedTx.Should().NotBeNull();
        retrievedTx!.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public async Task GetByAccountNumberAsync_ShouldReturnTransactions_WhenAccountNumberMatchesSourceOrDestination()
    {
        // Arrange
        var accountNumber = "111111";
        
        var txSource = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = accountNumber,
            DestinationAccountNumber = "222222",
            Amount = 100m,
            Currency = Currency.USD,
            Type = TransactionType.Withdrawal,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var txDest = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = "333333",
            DestinationAccountNumber = "111111",
            Amount = 200m,
            Currency = Currency.EUR,
            Type = TransactionType.Deposit,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var txUnrelated = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = "444444",
            DestinationAccountNumber = "555555",
            Amount = 50m,
            Currency = Currency.BRL,
            Type = TransactionType.Transfer,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(txSource);
        await _repository.SaveAsync(txDest);
        await _repository.SaveAsync(txUnrelated);

        // Act
        var result = await _repository.GetByAccountNumberAsync("111111");

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(txSource);
        result.Should().ContainEquivalentOf(txDest);
        result.Should().NotContainEquivalentOf(txUnrelated);
    }

    [Fact]
    public async Task GetByAccountNumberAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            SourceAccountNumber = "abcDeF",
            DestinationAccountNumber = "222222",
            Amount = 100m,
            Currency = Currency.USD,
            Type = TransactionType.Withdrawal,
            IdempotencyKey = Guid.NewGuid(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(tx);

        // Act
        var result = await _repository.GetByAccountNumberAsync("ABCDEF");

        // Assert
        result.Should().ContainEquivalentOf(tx);
    }
}

