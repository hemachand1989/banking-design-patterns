using BankingPatterns.Core.Entities;

namespace BankingPatterns.Patterns.Strategy;

/// <summary>
/// Strategy Pattern - Transaction Fee Calculation
/// 
/// REAL-WORLD USE CASE:
/// Different account types have different fee structures. Instead of having
/// complex if-else or switch statements, we use strategies that can be
/// easily swapped and tested independently.
/// 
/// INTERVIEW GOTCHAS:
/// 1. Q: "Why not use inheritance with Account having different fee methods?"
///    A: That violates Open/Closed Principle. Adding new account types requires
///       modifying existing code. Strategy allows adding new fee structures
///       without touching existing classes.
/// 
/// 2. Q: "Isn't this just using interfaces? What's special?"
///    A: Strategy is about encapsulating algorithms and making them interchangeable.
///       The key is runtime selection of behavior based on context.
/// 
/// 3. Q: "How do you choose which strategy to use?"
///    A: Typically through Factory pattern, DI container configuration, or
///       based on business rules (like account type).
/// 
/// 4. Q: "What are the downsides?"
///    A: - More classes to maintain
///       - Client must be aware of different strategies
///       - Can be overkill for simple algorithms
/// 
/// SOLID PRINCIPLES:
/// - Open/Closed: Open for extension (new strategies), closed for modification
/// - Single Responsibility: Each strategy handles one fee calculation algorithm
/// - Dependency Inversion: Depend on ITransactionFeeStrategy abstraction
/// </summary>
public interface ITransactionFeeStrategy
{
    decimal CalculateFee(Transaction transaction);
    string StrategyName { get; }
}
