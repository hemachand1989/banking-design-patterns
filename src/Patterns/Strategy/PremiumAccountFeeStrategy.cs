using BankingPatterns.Core.Entities;

namespace BankingPatterns.Patterns.Strategy;

/// <summary>
/// Premium Account Fee Strategy
/// 
/// BUSINESS RULE: Premium accounts have no transaction fees
/// but may have annual membership fees (handled separately).
/// 
/// INTERVIEW GOTCHA:
/// Q: "If premium has no fees, why have this class at all?"
/// A: 1. Explicit is better than implicit - makes intent clear
///    2. May need to add logic later (e.g., fee waivers with conditions)
///    3. Polymorphism - can be used interchangeably with other strategies
///    4. Null Object Pattern principle - better than returning null
/// </summary>
public class PremiumAccountFeeStrategy : ITransactionFeeStrategy
{
    public string StrategyName => "Premium Account Fee";

    public decimal CalculateFee(Transaction transaction)
    {
        // Premium accounts - no transaction fees
        // DESIGN NOTE: This could log premium transactions for analytics
        // even though fee is 0
        
        return 0m;
    }
}
