using System.Text.Json.Serialization;

namespace QtBank.Api.Domain.Models;

/// <summary>
/// Specifies the type of bank transaction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    /// <summary>
    /// Peer-to-peer money transfer.
    /// </summary>
    Transfer,

    /// <summary>
    /// Account balance deposit.
    /// </summary>
    Deposit,

    /// <summary>
    /// Account balance withdrawal.
    /// </summary>
    Withdrawal
}
