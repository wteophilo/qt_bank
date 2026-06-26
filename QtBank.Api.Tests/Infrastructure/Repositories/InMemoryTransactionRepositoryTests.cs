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
}
