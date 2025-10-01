using BankingPatterns.Core.Entities;

namespace BankingPatterns.Patterns.Strategy;

/// <summary>
/// Context class that uses the Strategy
/// 
/// INTERVIEW GOTCHA:
/// Q: "How would you make this thread-safe in a concurrent environment?"
/// A: 1. Make _feeStrategy readonly and set via constructor (immutability)
///    2. Or use ThreadLocal<T> if strategy needs to be thread-specific
///    3. Or use async-local context in async scenarios
///    4. Ensure strategies themselves are stateless (very important!)
/// 
/// Q: "How does this integrate with ASP.NET Core DI?"
/// A: Register strategies in DI container, inject IEnumerable<ITransactionFeeStrategy>
///    and select based on account type, or use factory pattern.
/// </summary>
public class TransactionProcessor
{
    private ITransactionFeeStrategy _feeStrategy;

    public TransactionProcessor(ITransactionFeeStrategy feeStrategy)
    {
        _feeStrategy = feeStrategy ?? throw new ArgumentNullException(nameof(feeStrategy));
    }

    // GOTCHA: Allowing runtime strategy change can cause issues
    // if not carefully managed (especially in concurrent scenarios)
    public void SetFeeStrategy(ITransactionFeeStrategy feeStrategy)
    {
        _feeStrategy = feeStrategy ?? throw new ArgumentNullException(nameof(feeStrategy));
    }

    public decimal ProcessTransaction(Transaction transaction)
    {
        // Calculate fee using current strategy
        var fee = _feeStrategy.CalculateFee(transaction);
        
        // PRODUCTION GOTCHA:
        // In real systems, you'd also:
        // 1. Validate transaction (separate concern - use Chain of Responsibility)
        // 2. Check account balance
        // 3. Apply fee to account
        // 4. Create audit trail (use Command pattern)
        // 5. Publish domain event (use Observer/Mediator pattern)
        // 6. Handle idempotency (critical for financial transactions!)
        
        transaction.Fee = fee;
        transaction.Status = TransactionStatus.Completed;
        
        return fee;
    }
}

/// <summary>
/// BONUS: Factory to select appropriate strategy
/// This shows how Strategy is often used with Factory pattern
/// </summary>
public static class FeeStrategyFactory
{
    // INTERVIEW GOTCHA:
    // Q: "Isn't this a code smell with the switch statement?"
    // A: Not necessarily. This is a controlled point of variation.
    //    The alternative is to use a dictionary or DI container.
    //    The key is that adding new account types only changes THIS factory,
    //    not the strategies themselves.
    
    public static ITransactionFeeStrategy GetStrategy(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Savings => new SavingsAccountFeeStrategy(),
            AccountType.Checking => new CheckingAccountFeeStrategy(),
            AccountType.Premium => new PremiumAccountFeeStrategy(),
            AccountType.Investment => new CheckingAccountFeeStrategy(), // Same as checking for this example
            _ => throw new ArgumentException($"Unknown account type: {accountType}")
        };
    }
    
    // STAFF ENGINEER LEVEL: Discuss caching strategies
    // Since strategies are stateless, we can cache instances
    private static readonly Dictionary<AccountType, ITransactionFeeStrategy> _strategyCache = new()
    {
        { AccountType.Savings, new SavingsAccountFeeStrategy() },
        { AccountType.Checking, new CheckingAccountFeeStrategy() },
        { AccountType.Premium, new PremiumAccountFeeStrategy() },
        { AccountType.Investment, new CheckingAccountFeeStrategy() }
    };
    
    public static ITransactionFeeStrategy GetCachedStrategy(AccountType accountType)
    {
        // GOTCHA: In high-concurrency scenarios, Dictionary reads are thread-safe
        // but if you modify it, you need synchronization or use ConcurrentDictionary
        return _strategyCache.TryGetValue(accountType, out var strategy)
            ? strategy
            : throw new ArgumentException($"Unknown account type: {accountType}");
    }
}
