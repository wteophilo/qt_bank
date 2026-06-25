using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using QtBank.Api.Application.DTOs;
using QtBank.Api.Domain.Events;
using QtBank.Api.Domain.Models;
using QtBank.Api.Domain.Repositories;
using QtBank.Api.Infrastructure.Messaging;

namespace QtBank.Api.Application.Accounts.Commands;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _repository;
    private readonly IPubSubPublisher _publisher;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository repository,
        IPubSubPublisher publisher,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing CreateAccountCommand for owner: {OwnerName} with balance: {Balance}",
            request.OwnerName, request.Balance);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = request.AccountNumber.Trim(),
            Balance = request.Balance,
            OwnerName = request.OwnerName.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = request.Status
        };

        var savedAccount = await _repository.SaveAsync(account, cancellationToken);
        _logger.LogInformation("Account {AccountId} saved successfully.", savedAccount.Id);

        var accountCreatedEvent = new AccountCreated(
            savedAccount.Id,
            savedAccount.AccountNumber,
            savedAccount.Balance,
            savedAccount.OwnerName,
            savedAccount.CreatedAt,
            savedAccount.Status
        );

        await _publisher.PublishAsync("accounts-topic", accountCreatedEvent, cancellationToken);
        _logger.LogInformation("AccountCreated event published for account {AccountId}.", savedAccount.Id);

        return new AccountDto(
            savedAccount.Id,
            savedAccount.AccountNumber,
            savedAccount.Balance,
            savedAccount.OwnerName,
            savedAccount.CreatedAt,
            savedAccount.Status.ToString()
        );
    }
}
