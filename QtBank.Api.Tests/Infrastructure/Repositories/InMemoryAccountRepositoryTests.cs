using System;
using System.Threading.Tasks;
using FluentAssertions;
using QtBank.Api.Domain.Models;
using QtBank.Api.Infrastructure.Repositories;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Repositories;

public class InMemoryAccountRepositoryTests
{
    private readonly InMemoryAccountRepository _repository;

    public InMemoryAccountRepositoryTests()
    {
        _repository = new InMemoryAccountRepository();
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreAccount_AndGetByIdShouldRetrieveIt()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1010-1",
            Balance = 500.00m,
            OwnerName = "Jane Doe",
            CreatedAt = DateTime.UtcNow,
            Status = AccountStatus.Active
        };

        // Act
        var savedAccount = await _repository.SaveAsync(account);
        var retrievedAccount = await _repository.GetByIdAsync(account.Id);

        // Assert
        savedAccount.Should().BeEquivalentTo(account);
        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Should().BeEquivalentTo(account);
    }

    [Fact]
    public async Task GetByNumberAsync_ShouldReturnAccount_CaseInsensitively()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "Acct-999",
            Balance = 1000m,
            OwnerName = "Bob Smith",
            CreatedAt = DateTime.UtcNow,
            Status = AccountStatus.Active
        };
        await _repository.SaveAsync(account);

        // Act & Assert
        var exactMatch = await _repository.GetByNumberAsync("Acct-999");
        var lowerMatch = await _repository.GetByNumberAsync("acct-999");
        var upperMatch = await _repository.GetByNumberAsync("ACCT-999");
        var nonExistent = await _repository.GetByNumberAsync("Acct-000");

        exactMatch.Should().NotBeNull();
        exactMatch!.Id.Should().Be(account.Id);

        lowerMatch.Should().NotBeNull();
        lowerMatch!.Id.Should().Be(account.Id);

        upperMatch.Should().NotBeNull();
        upperMatch!.Id.Should().Be(account.Id);

        nonExistent.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSavedAccounts()
    {
        // Arrange
        var acc1 = new Account { Id = Guid.NewGuid(), AccountNumber = "1" };
        var acc2 = new Account { Id = Guid.NewGuid(), AccountNumber = "2" };

        await _repository.SaveAsync(acc1);
        await _repository.SaveAsync(acc2);

        // Act
        var allAccounts = await _repository.GetAllAsync();

        // Assert
        allAccounts.Should().HaveCount(2);
        allAccounts.Should().ContainEquivalentOf(acc1);
        allAccounts.Should().ContainEquivalentOf(acc2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAccountAndReturnTrue_WhenAccountExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var account = new Account { Id = id, AccountNumber = "123" };
        await _repository.SaveAsync(account);

        // Act
        var deleteResult = await _repository.DeleteAsync(id);
        var retrieved = await _repository.GetByIdAsync(id);

        // Assert
        deleteResult.Should().BeTrue();
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenAccountDoesNotExist()
    {
        // Act
        var deleteResult = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        deleteResult.Should().BeFalse();
    }
}
