using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace QtBank.Api.Infrastructure.Telemetry;

/// <summary>
/// Domain-independent helper to record key application metrics.
/// </summary>
public sealed class ApplicationMetrics
{
    public const string MeterName = "QtBank.Api.Metrics";
    private readonly Meter _meter;
    
    private readonly Counter<long> _transactionsTotal;
    private readonly Histogram<double> _transactionAmount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetrics"/> class.
    /// </summary>
    public ApplicationMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");
        
        // Counter metric to track overall transactions by type and outcome status
        _transactionsTotal = _meter.CreateCounter<long>(
            "qtbank_transactions_total",
            description: "Total number of requested transactions (transfers, deposits, withdrawals).");

        // Histogram metric to track distribution of transaction amounts in USD
        _transactionAmount = _meter.CreateHistogram<double>(
            "qtbank_transaction_amount_usd",
            unit: "USD",
            description: "The distribution of transaction amounts by type.");
    }

    /// <summary>
    /// Records a transaction occurrence, tracking type, amount, and completion status.
    /// </summary>
    /// <param name="type">The type of transaction (e.g. Transfer, Deposit, Withdrawal).</param>
    /// <param name="status">The final outcome of the transaction (e.g. Success, Failed).</param>
    /// <param name="amount">The numeric amount of money involved.</param>
    public void RecordTransaction(string type, string status, double amount)
    {
        _transactionsTotal.Add(1, 
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("status", status)
        );
        
        _transactionAmount.Record(amount, 
            new KeyValuePair<string, object?>("type", type)
        );
    }
}
