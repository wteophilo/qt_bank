using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QtBank.Api.Domain.Models;

namespace QtBank.Api.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account> SaveAsync(Account account, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
