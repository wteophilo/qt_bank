using System.Text.Json.Serialization;

namespace QtBank.Api.Domain.Models;

/// <summary>
/// Supported currencies for bank transactions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Currency
{
    /// <summary>
    /// Brazilian Real.
    /// </summary>
    BRL,

    /// <summary>
    /// United States Dollar.
    /// </summary>
    USD,

    /// <summary>
    /// Euro.
    /// </summary>
    EUR,

    /// <summary>
    /// Canadian Dollar.
    /// </summary>
    CAD
}
