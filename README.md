# Banking Design Patterns - Staff Engineer Interview Prep 🎯

> **Real-world C# .NET Core design patterns with banking examples, interview gotchas, and expert insights**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🎯 Why This Repository?

This repository is specifically designed for **Staff Engineer interviews** focusing on design patterns in banking/financial systems. Each pattern includes:

- ✅ **Production-ready C# .NET Core implementation**
- 💡 **Real banking use cases** that interviewers love
- ⚠️ **Common gotchas** that will come up in interviews
- 🎤 **Expected interview questions** with model answers
- 🔍 **Thread safety, performance, and scalability considerations**

## 📚 Documentation

- **[INTERVIEW_GUIDE.md](INTERVIEW_GUIDE.md)** - Comprehensive guide with all patterns, code examples, and detailed gotchas
- **[CHEAT_SHEET.md](CHEAT_SHEET.md)** - Quick reference for last-minute review before interview
- **[src/](src/)** - Production-ready C# implementations

## 🏗️ Patterns Covered

| Pattern | Real-World Use Case | Key Interview Question |
|---------|---------------------|------------------------|
| **Strategy** | Transaction fee calculation per account type | "Why not just use inheritance?" |
| **Factory** | Account creation with validation | "Factory Method vs Abstract Factory?" |
| **Decorator** | Transaction pipeline (fraud, logging, notify) | "Why not inheritance?" |
| **Chain of Responsibility** | Loan approval levels (Clerk→Manager→VP) | "What if no handler processes?" |
| **Observer** | Balance change notifications | "How prevent memory leaks?" |
| **Repository** | Data access abstraction | "Anti-pattern with EF Core?" |
| **Unit of Work** | Multi-account transaction consistency | "Nested transactions?" |
| **Singleton** | Configuration (DON'T USE - use DI!) | "Why is it anti-pattern?" |
| **Adapter** | Payment gateway integration | "Adapter vs Facade?" |
| **Command** | Transaction with audit trail & undo | "How handle undo in banking?" |

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/hemachand1989/banking-design-patterns.git
cd banking-design-patterns

# Review the code
cd src/
code .

# Study the interview guide
cat INTERVIEW_GUIDE.md

# Last-minute review
cat CHEAT_SHEET.md
```

## 💎 What Makes This Different?

### 1. Banking-Specific Gotchas
```csharp
// ❌ Most tutorials show this (WRONG for banking!)
public async Task UndoAsync()
{
    _account.Balance += _amount; // Just reverse it
}

// ✅ We show the CORRECT approach
public async Task UndoAsync()
{
    // In banking, create compensating transaction
    var reversal = new Transaction
    {
        Type = TransactionType.Reversal,
        OriginalId = _transactionId,
        Amount = _amount
    };
    await _repository.AddAsync(reversal); // Audit trail maintained!
}
```

### 2. Interview-Ready Comments
Every file includes:
- Why this pattern was chosen
- Alternative approaches and trade-offs
- Common interview questions
- Thread safety considerations
- Performance implications

### 3. Staff Engineer Perspective
Goes beyond basic implementation to discuss:
- Distributed systems considerations
- Scalability patterns
- Event sourcing and CQRS
- Circuit breakers and saga patterns
- Monitoring and observability

## 🎤 Sample Interview Questions Covered

### Strategy Pattern
- **Q**: "Why not use inheritance with Account having different fee methods?"
- **A**: Violates Open/Closed Principle. Strategy allows adding new fee structures without modifying existing classes. ✓

### Observer Pattern
- **Q**: "How do you prevent memory leaks with observers?"
- **A**: Always implement Detach/Unsubscribe, use weak references, implement IDisposable for subscriptions. ✓

### Repository Pattern
- **Q**: "Isn't Repository an anti-pattern with EF Core?"
- **A**: EF Core DbSet is already Repository. Add when: multiple data sources, complex queries, or hiding ORM from domain. ✓

### Command Pattern
- **Q**: "How do you handle undo in financial systems?"
- **A**: Can't delete/undo in banking. Create compensating transactions for audit trail. ✓

## 📖 Study Path

### Day Before Interview:
1. Review [CHEAT_SHEET.md](CHEAT_SHEET.md) (30 min)
2. Study top 10 gotchas
3. Review pattern selection matrix
4. Practice explaining trade-offs

### Week Before Interview:
1. Read [INTERVIEW_GUIDE.md](INTERVIEW_GUIDE.md) thoroughly
2. Implement 2-3 patterns yourself
3. Review Strategy pattern code (most common)
4. Study SOLID principles mapping

### What Interviewers Look For:
- ✅ Explain **WHY** you chose a pattern (not just how)
- ✅ Discuss **trade-offs** and alternatives
- ✅ Know when **NOT** to use a pattern
- ✅ Consider **scalability** and **performance**
- ✅ Think about **distributed systems**
- ✅ Address **thread safety** and **concurrency**

## 🔥 Top 3 Gotchas You MUST Know

### 1. Observer Pattern Memory Leaks
```csharp
// ALWAYS provide unsubscribe!
public void Detach(IObserver observer) 
{
    lock (_lock) 
    {
        _observers.Remove(observer);
    }
}
```

### 2. Singleton is Anti-Pattern
```csharp
// ❌ DON'T do this
public static ConfigManager Instance { get; }

// ✅ DO this instead
services.AddSingleton<IConfigService, ConfigService>();
```

### 3. Banking Undo = Compensating Transaction
```csharp
// Never delete/modify - always create offsetting entry!
var reversal = new Transaction
{
    Type = TransactionType.Reversal,
    OriginalTransactionId = _transactionId
};
```

## 🎯 Key Banking Concepts

| Concept | Explanation | Interview Importance |
|---------|-------------|---------------------|
| **Idempotency** | Same operation multiple times = same result | ⭐⭐⭐⭐⭐ |
| **Audit Trail** | Every transaction must be traceable | ⭐⭐⭐⭐⭐ |
| **Compensation** | Can't delete, must reverse transactions | ⭐⭐⭐⭐⭐ |
| **Consistency** | ACID properties in transactions | ⭐⭐⭐⭐ |
| **Thread Safety** | Handle concurrent operations safely | ⭐⭐⭐⭐ |

## 💼 Real Interview Dialogue Example

**Interviewer**: "How would you implement different fee structures for account types?"

**You**: "I'd use the Strategy pattern because:
1. **Open/Closed Principle** - add new account types without modifying existing code
2. **Testability** - each strategy can be unit tested independently
3. **Runtime selection** - choose strategy based on account type
4. **Performance** - cache stateless strategy instances

Key gotcha: strategies MUST be stateless for thread safety. Any state passes as parameters.

For ASP.NET Core, I'd register strategies in DI and use a factory method to select the appropriate strategy."

**Interviewer**: 😊 "Great! What if fee calculation becomes very complex?"

**You**: "Several options:
- Chain of Responsibility within the strategy for multiple rules
- Specification pattern for complex business rules
- Rules engine if rules change frequently
- Keep strategies composable

In banking, I'd lean toward rules engine for dynamic rules that business analysts can modify."

## ⚠️ Common Mistakes to Avoid

1. **Using Singleton everywhere** - Use DI with singleton lifetime instead
2. **Over-decorating** - Keep chains under 5 layers
3. **Forgetting thread safety** - Critical in Observer and Singleton
4. **Not handling exceptions** - Always consider failure scenarios
5. **Ignoring async/await** - Modern C# is async-first
6. **Leaky abstractions** - Repository returning IQueryable
7. **True undo in banking** - Use compensating transactions
8. **Nested transactions** - Most DBs don't support them
9. **Lost requests in Chain** - Always have final handler
10. **Memory leaks in Observer** - Always allow unsubscribe

## 🏆 Staff Engineer Expectations

**Beyond basic implementation:**
- Explain **architectural decisions** and trade-offs
- Discuss **scalability** (10x, 100x, 1000x users)
- Consider **distributed systems** (microservices, event-driven)
- Think about **observability** (logging, metrics, tracing)
- Understand **modern alternatives** (MediatR, middleware, CQRS)
- Know when to **break patterns** for practical reasons

## 📊 Pattern Selection Quick Reference

```
Need runtime algorithm selection? → Strategy
Multiple approval levels? → Chain of Responsibility
Event notifications? → Observer (or MediatR)
Add features dynamically? → Decorator (or Middleware)
Abstract data access? → Repository (if complex)
Coordinate multiple repos? → Unit of Work
Interface compatibility? → Adapter
Complex object creation? → Factory
Audit trail/undo? → Command
Single instance? → DON'T USE SINGLETON - Use DI!
```

## 🎓 Additional Resources

- [Microsoft .NET Design Patterns](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [Refactoring Guru - Design Patterns](https://refactoring.guru/design-patterns)
- [Martin Fowler - Patterns](https://martinfowler.com/articles/enterprisePatterns.html)

## 📝 License

MIT License - Feel free to use for interview preparation

## 🤝 Contributing

Found a gotcha I missed? Have a better example? PRs welcome!

---

**Remember**: Staff engineers explain **WHY** and **WHEN**, not just **HOW**!

**Good luck with your interview!** 🚀💪

*Star this repo if it helps you ace your interview!* ⭐
