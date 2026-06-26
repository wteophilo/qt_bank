using System.Text.Json.Serialization;

namespace QtBank.Api.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}
