using BankingPatterns.Core.Entities;

namespace BankingPatterns.Patterns.Strategy;

/// <summary>
/// Savings Account Fee Strategy
/// 
/// BUSINESS RULE: Savings accounts have no fees for deposits,
/// but charge $1 for withdrawals over 5 per month.
/// 
/// INTERVIEW GOTCHA:
/// Q: "How would you track the number of monthly withdrawals?"
/// A: This would require state management - could be stored in Account entity,
///    or retrieved from transaction history. In real systems, this would be
///    a separate service concern (separation of concerns principle).
/// </summary>
public class SavingsAccountFeeStrategy : ITransactionFeeStrategy
{
    public string StrategyName => "Savings Account Fee";

    public decimal CalculateFee(Transaction transaction)
    {
        // GOTCHA: This is simplified. In production:
        // 1. You'd check monthly withdrawal count
        // 2. Possibly use a repository to get transaction history
        // 3. Consider timezone issues for "monthly" calculation
        // 4. Handle fee waivers for premium customers
        
        return transaction.Type switch
        {
            TransactionType.Deposit => 0m,
            TransactionType.Withdrawal => 1.00m, // Simplified: assume fee applies
            TransactionType.Transfer => 0.50m,
            TransactionType.Payment => 0.50m,
            _ => 0m
        };
    }
}
