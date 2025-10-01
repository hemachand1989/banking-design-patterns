namespace BankingPatterns.Core.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }

    public Transaction(Guid accountId, TransactionType type, decimal amount, string description = "")
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Description = description;
        Timestamp = DateTime.UtcNow;
        Status = TransactionStatus.Pending;
    }
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Payment
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Reversed
}
