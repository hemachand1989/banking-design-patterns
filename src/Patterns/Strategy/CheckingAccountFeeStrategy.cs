using BankingPatterns.Core.Entities;

namespace BankingPatterns.Patterns.Strategy;

/// <summary>
/// Checking Account Fee Strategy
/// 
/// BUSINESS RULE: Checking accounts charge 0.5% of transaction amount
/// with a minimum of $0.50 and maximum of $5.00.
/// 
/// INTERVIEW GOTCHA:
/// Q: "What if the calculation becomes more complex with multiple conditions?"
/// A: Consider using Chain of Responsibility or Specification pattern within
///    the strategy. Or, use a rule engine for complex business rules.
/// </summary>
public class CheckingAccountFeeStrategy : ITransactionFeeStrategy
{
    private const decimal FeePercentage = 0.005m; // 0.5%
    private const decimal MinFee = 0.50m;
    private const decimal MaxFee = 5.00m;

    public string StrategyName => "Checking Account Fee";

    public decimal CalculateFee(Transaction transaction)
    {
        // Calculate percentage-based fee
        var fee = transaction.Amount * FeePercentage;
        
        // Apply min/max bounds
        // GOTCHA: Math.Clamp is .NET 6+, be aware of framework version
        fee = Math.Clamp(fee, MinFee, MaxFee);
        
        // PRODUCTION CONSIDERATION:
        // Should fees be calculated differently for different transaction types?
        // This could be extracted to another strategy hierarchy.
        
        return fee;
    }
}
