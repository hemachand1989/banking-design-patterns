namespace BankingPatterns.Core.Entities;

public class Account
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Balance { get; private set; }
    public AccountType AccountType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }

    public Account(string accountNumber, string customerName, AccountType accountType, decimal initialBalance = 0)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        CustomerName = customerName;
        AccountType = accountType;
        Balance = initialBalance;
        CreatedDate = DateTime.UtcNow;
        LastModifiedDate = DateTime.UtcNow;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive", nameof(amount));

        Balance += amount;
        LastModifiedDate = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
        LastModifiedDate = DateTime.UtcNow;
    }
}

public enum AccountType
{
    Savings,
    Checking,
    Investment,
    Premium
}
