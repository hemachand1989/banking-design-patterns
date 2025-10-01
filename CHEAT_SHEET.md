Transaction pipeline (all layers process)

### Q5: "Explain idempotency in banking transactions"
**A:** Critical concept! Same operation multiple times produces same result. Implementation:
- Use unique transaction IDs
- Check if transaction already processed before executing
- Use database constraints (unique indexes)
- Store operation results
- Essential for retry logic and message queues

### Q6: "How do you handle concurrent withdrawals from same account?"
**A:** 
1. **Pessimistic locking**: Lock row during transaction
2. **Optimistic locking**: Use row version/timestamp, retry on conflict
3. **Event sourcing**: Append-only log, no updates
4. **In banking**: Usually pessimistic for accuracy, optimistic for performance

```csharp
// Optimistic with EF Core
public class Account
{
    public byte[] RowVersion { get; set; } // Timestamp in SQL Server
}

// On conflict, EF throws DbUpdateConcurrencyException
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Reload and retry
}
```

### Q7: "Factory Method vs Abstract Factory vs Simple Factory?"
**A:**
- **Simple Factory**: Not GoF pattern, but practical. Single method returns objects.
- **Factory Method**: Subclasses decide which class to instantiate. One product.
- **Abstract Factory**: Creates families of related products. Multiple products.
- **In banking**: Usually Simple Factory is enough!

### Q8: "How does Unit of Work handle multiple DbContexts?"
**A:** 
- Single DbContext = built-in UoW (recommended)
- Multiple DbContexts = use TransactionScope for distributed transaction
- **Better**: Reconsider bounded contexts if needing multiple contexts
- **Alternative**: Use outbox pattern for eventual consistency

### Q9: "When would you use Command pattern in banking?"
**A:**
1. Audit trail - every command logged
2. Queuing - deferred execution
3. Transaction reversal - compensating commands
4. Event sourcing - store commands not state
5. CQRS - separate command from query models

### Q10: "What's the difference between Adapter and Facade?"
**A:**
- **Adapter**: Converts one interface to another (like plug adapter)
- **Facade**: Simplifies complex subsystem (like universal remote)
- **Adapter**: Focus on compatibility
- **Facade**: Focus on simplification
- **Example**: Stripe adapter (convert interface) vs Payment facade (simplify multiple gateways)

## 🚀 Staff Engineer Level Concepts

### 1. **Distributed Systems Considerations**
```csharp
// Singleton doesn't work across servers!
// ❌ BAD in microservices
public static class ConfigManager
{
    private static Config _config;
}

// ✅ GOOD: Use distributed cache
public class ConfigService
{
    private readonly IDistributedCache _cache;
    
    public async Task<Config> GetConfigAsync()
    {
        return await _cache.GetAsync<Config>("app:config") 
            ?? await LoadFromSourceAsync();
    }
}
```

### 2. **Event Sourcing + CQRS**
```csharp
// Instead of storing current state, store events
public class AccountAggregate
{
    private List<IEvent> _events = new();
    
    public void Withdraw(decimal amount)
    {
        // Validate
        var evt = new MoneyWithdrawnEvent(Id, amount, DateTime.UtcNow);
        Apply(evt);
        _events.Add(evt); // Store event
    }
    
    private void Apply(MoneyWithdrawnEvent evt)
    {
        Balance -= evt.Amount; // Rebuild state from events
    }
}
```

### 3. **Saga Pattern for Distributed Transactions**
```csharp
// Long-running transaction across services
public class TransferSaga
{
    public async Task ExecuteAsync(TransferCommand cmd)
    {
        try
        {
            await _accountService.DebitAsync(cmd.FromAccount, cmd.Amount);
            await _accountService.CreditAsync(cmd.ToAccount, cmd.Amount);
            await _notificationService.SendAsync(cmd);
        }
        catch
        {
            // Compensating transactions
            await _accountService.CreditAsync(cmd.FromAccount, cmd.Amount);
            throw;
        }
    }
}
```

### 4. **Circuit Breaker for External Services**
```csharp
// Prevent cascading failures
public class PaymentGatewayAdapter : IPaymentGateway
{
    private readonly ICircuitBreaker _circuitBreaker;
    
    public async Task<PaymentResult> ProcessAsync(decimal amount, string account)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _gateway.ProcessAsync(amount, account);
        });
    }
}
```

### 5. **Outbox Pattern for Reliable Messaging**
```csharp
// Ensure message sent even if process crashes
public async Task TransferAsync(Transfer transfer)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    
    // 1. Update database
    await _context.Accounts.UpdateAsync(transfer.FromAccount);
    await _context.Accounts.UpdateAsync(transfer.ToAccount);
    
    // 2. Write to outbox table (same transaction)
    var outboxMessage = new OutboxMessage
    {
        Type = "TransferCompleted",
        Payload = JsonSerializer.Serialize(transfer)
    };
    await _context.Outbox.AddAsync(outboxMessage);
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    
    // 3. Background worker reads outbox and publishes to message bus
}
```

## 📊 Performance Considerations

| Pattern | Performance Impact | Optimization |
|---------|-------------------|--------------|
| Strategy | Negligible | Cache stateless instances |
| Decorator | Each layer = overhead | Keep chains short (< 5 layers) |
| Chain of Responsibility | O(n) traversal | Short chains, cache routing |
| Observer | All observers notified | Async notifications, parallel |
| Repository | Query performance | Use specific methods, avoid IQueryable |
| Factory | Reflection = slow | Cache instances, use DI |
| Command | Serialization overhead | Use binary format for high throughput |

## 🔒 Security Considerations

### Pattern-Specific Security:
1. **Strategy**: Validate strategy selection, don't trust user input
2. **Observer**: Be careful with sensitive data in notifications
3. **Adapter**: Sanitize data crossing boundaries
4. **Command**: Authorize commands before execution
5. **Repository**: Prevent SQL injection with parameterized queries

```csharp
// Authorization with Command pattern
public class AuthorizedCommandDecorator : ICommand
{
    private readonly ICommand _command;
    private readonly IAuthorizationService _authService;
    
    public async Task ExecuteAsync()
    {
        if (!await _authService.AuthorizeAsync(_command))
            throw new UnauthorizedException();
            
        await _command.ExecuteAsync();
    }
}
```

## 🎯 Interview Pro Tips

### Do's:
✅ Explain WHY you chose a pattern, not just HOW it works
✅ Discuss trade-offs and alternatives
✅ Mention when NOT to use a pattern
✅ Talk about testing strategy
✅ Consider scalability and performance
✅ Think about distributed systems
✅ Use real banking examples

### Don'ts:
❌ Force patterns where they don't fit
❌ Ignore SOLID principles
❌ Forget about thread safety
❌ Overlook error handling
❌ Ignore async/await in modern C#
❌ Forget about observability/logging
❌ Use patterns you don't understand

## 🔥 Last-Minute Review

### 3 Most Important Patterns for Banking:
1. **Strategy**: Fee calculations, interest rates
2. **Command**: Audit trail, transaction reversal
3. **Repository + UoW**: Data access and consistency

### 3 Most Likely Gotcha Questions:
1. "Why is Singleton an anti-pattern?" → Use DI instead
2. "How do you prevent memory leaks with Observer?" → Always unsubscribe
3. "How do you handle undo in financial systems?" → Compensating transactions, not true undo

### 3 Key Banking Concepts:
1. **Idempotency**: Same operation = same result
2. **Audit Trail**: Every transaction must be traceable
3. **Compensation**: Can't delete/undo, must reverse

### 3 Thread Safety Rules:
1. Make strategies/decorators stateless
2. Lock when modifying observer lists
3. Use optimistic/pessimistic locking for account updates

### 3 SOLID Principles to Mention:
1. **Open/Closed**: Strategy and Chain of Responsibility
2. **Single Responsibility**: Each pattern class has one job
3. **Dependency Inversion**: Depend on abstractions (interfaces)

## 💡 Bonus: Modern Alternatives

| Pattern | Modern Alternative | When to Use Alternative |
|---------|-------------------|-------------------------|
| Singleton | DI with singleton lifetime | Always |
| Observer | MediatR, message bus | Loose coupling needed |
| Command | MediatR.IRequest | CQRS architecture |
| Chain of Responsibility | ASP.NET Middleware | Request pipeline |
| Factory | DI container | Simple scenarios |
| Repository + UoW | EF Core directly | Simple CRUD only |

## 🎬 Sample Interview Dialogue

**Interviewer**: "How would you implement different fee structures for different account types?"

**You**: "I'd use the **Strategy pattern**. Each account type gets its own fee calculation strategy implementing a common `ITransactionFeeStrategy` interface. This follows the **Open/Closed Principle** - we can add new account types without modifying existing code.

For performance, since strategies are stateless, I'd cache the instances in a factory. In ASP.NET Core, I'd register them in DI and use a factory method to select the right strategy based on account type.

One gotcha to watch for: strategies must be **thread-safe**, which means they should be stateless. Any state should be passed as parameters.

For testing, this design makes it easy to mock the strategy interface and test the transaction processor independently."

**Interviewer**: "What if fee calculation becomes very complex with many conditions?"

**You**: "If a single strategy gets too complex, I have a few options:
1. Use **Chain of Responsibility** within the strategy to apply multiple rules
2. Use **Specification pattern** for complex business rules
3. Consider a **Rules Engine** for highly dynamic rules
4. Break into smaller, composable strategies using **Decorator pattern**

In banking, I'd lean toward the rules engine if rules change frequently, as it allows business analysts to modify rules without code changes."

---

## ✅ Final Checklist

Before interview:
- [ ] Review Strategy pattern code
- [ ] Understand when NOT to use Repository with EF Core
- [ ] Know memory leak prevention with Observer
- [ ] Explain idempotency and why it matters
- [ ] Understand difference between Adapter and Facade
- [ ] Know how to handle undo in banking (compensating transactions)
- [ ] Review SOLID principles mapping to patterns
- [ ] Understand thread safety implications
- [ ] Know modern alternatives (DI, MediatR, Middleware)
- [ ] Practice explaining trade-offs

**Remember**: Staff engineers explain WHY and WHEN, not just HOW!

Good luck! 🚀💪
