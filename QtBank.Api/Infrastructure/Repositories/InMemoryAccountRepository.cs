using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;

namespace QtBank.Api.Infrastructure.Repositories;

public class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<Guid, Account> _accounts = new();

    public Task<Account> SaveAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id] = account;
        return Task.FromResult(account);
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _accounts.TryGetValue(id, out var account);
        return Task.FromResult(account);
    }

    public Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = _accounts.Values.FirstOrDefault(a => string.Equals(a.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(account);
    }

    public Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Account>>(_accounts.Values);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _accounts.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}
