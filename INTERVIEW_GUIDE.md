void HandleRequest(LoanApplication loan)
    {
        if (loan.Amount <= ApprovalLimit && loan.CreditScore >= 650)
        {
            loan.Status = LoanStatus.Approved;
            loan.ApprovedBy = "Clerk";
        }
        else if (_nextHandler != null)
        {
            _nextHandler.HandleRequest(loan);
        }
        else
        {
            // CRITICAL: Never drop requests in banking!
            loan.Status = LoanStatus.Rejected;
        }
    }
}
```

---

## Pattern 3: Observer Pattern
**Use Case**: Account Balance Change Notifications

### Interview Questions to Expect:
**Q: "How do you prevent memory leaks with observers?"**
A: CRITICAL GOTCHA!
- Always implement IDisposable for subscriptions
- Use weak references for observers
- Explicit Unsubscribe in finally blocks
- Consider using WeakEventManager (.NET)
- In ASP.NET, be careful with scoped vs singleton observers

**Q: "How does this differ from event aggregator/mediator?"**
A:
- Observer: Direct subject-observer coupling
- Mediator: Decoupled through central hub
- Event Aggregator: Pub/sub with loose coupling
- In modern apps, prefer MediatR or message bus for loose coupling

**Q: "Thread safety concerns?"**
A:
- Lock when adding/removing observers
- Use concurrent collections (ConcurrentBag)
- Consider making notifications async
- Be aware of deadlocks if observer calls back to subject

### Implementation Pattern:
```csharp
public interface IAccountObserver
{
    void OnBalanceChanged(Account account, decimal oldBalance, decimal newBalance);
}

public class Account
{
    private readonly List<IAccountObserver> _observers = new();
    private readonly object _lock = new object();
    
    public void Attach(IAccountObserver observer)
    {
        lock (_lock)
        {
            _observers.Add(observer);
        }
    }
    
    public void Detach(IAccountObserver observer)
    {
        lock (_lock)
        {
            _observers.Remove(observer); // GOTCHA: Always allow unsubscribe!
        }
    }
    
    private void NotifyObservers(decimal oldBalance, decimal newBalance)
    {
        // GOTCHA: Copy list to avoid modification during iteration
        IAccountObserver[] observersCopy;
        lock (_lock)
        {
            observersCopy = _observers.ToArray();
        }
        
        foreach (var observer in observersCopy)
        {
            // PRODUCTION: Wrap in try-catch, one observer shouldn't break others
            try
            {
                observer.OnBalanceChanged(this, oldBalance, newBalance);
            }
            catch (Exception ex)
            {
                // Log but don't propagate
            }
        }
    }
}
```

---

## Pattern 4: Decorator Pattern
**Use Case**: Transaction Processing Pipeline (Logging, Fraud Detection, Notifications)

### Interview Questions to Expect:
**Q: "Why not just use inheritance?"**
A:
- Inheritance is static, can't add/remove at runtime
- Multiple features = explosion of subclasses (2^n combinations)
- Decorators can be composed dynamically
- Follows Open/Closed Principle

**Q: "How many decorators is too many?"**
A:
- Watch for performance - each decorator adds overhead
- Consider pipeline/chain if order matters
- More than 5-7 decorators gets hard to debug
- Use middleware pattern for ordered processing

**Q: "How does this differ from Proxy pattern?"**
A:
- Decorator: Adds behavior/features
- Proxy: Controls access (lazy loading, security, caching)
- Similar structure, different intent

### Implementation Pattern:
```csharp
public interface ITransactionProcessor
{
    Task<bool> ProcessAsync(Transaction transaction);
}

// Base implementation
public class BasicTransactionProcessor : ITransactionProcessor
{
    public async Task<bool> ProcessAsync(Transaction transaction)
    {
        // Core transaction logic
        transaction.Status = TransactionStatus.Completed;
        return true;
    }
}

// Decorator base
public abstract class TransactionProcessorDecorator : ITransactionProcessor
{
    protected readonly ITransactionProcessor _inner;
    
    protected TransactionProcessorDecorator(ITransactionProcessor inner)
    {
        _inner = inner;
    }
    
    public abstract Task<bool> ProcessAsync(Transaction transaction);
}

// Fraud detection decorator
public class FraudDetectionDecorator : TransactionProcessorDecorator
{
    public FraudDetectionDecorator(ITransactionProcessor inner) : base(inner) { }
    
    public override async Task<bool> ProcessAsync(Transaction transaction)
    {
        // BEFORE: Check for fraud
        if (await IsFraudulentAsync(transaction))
        {
            transaction.Status = TransactionStatus.Failed;
            return false;
        }
        
        // Delegate to wrapped processor
        var result = await _inner.ProcessAsync(transaction);
        
        // AFTER: Additional fraud checks if needed
        return result;
    }
    
    private async Task<bool> IsFraudulentAsync(Transaction transaction)
    {
        // Fraud detection logic
        return false;
    }
}

// Logging decorator
public class LoggingDecorator : TransactionProcessorDecorator
{
    private readonly ILogger _logger;
    
    public LoggingDecorator(ITransactionProcessor inner, ILogger logger) : base(inner)
    {
        _logger = logger;
    }
    
    public override async Task<bool> ProcessAsync(Transaction transaction)
    {
        _logger.LogInformation($"Processing transaction {transaction.Id}");
        
        var result = await _inner.ProcessAsync(transaction);
        
        _logger.LogInformation($"Transaction {transaction.Id} result: {result}");
        
        return result;
    }
}

// DI Registration (ASP.NET Core)
services.AddScoped<ITransactionProcessor>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LoggingDecorator>>();
    
    // Build the decorator chain
    ITransactionProcessor processor = new BasicTransactionProcessor();
    processor = new FraudDetectionDecorator(processor);
    processor = new LoggingDecorator(processor, logger);
    
    return processor;
});
```

---

## Pattern 5: Repository Pattern
**Use Case**: Data Access Abstraction

### Interview Questions to Expect:
**Q: "Isn't Repository an anti-pattern with EF Core?"**
A: IMPORTANT DEBATE!
- EF Core DbSet already implements Repository/UoW
- Adding repository on top = extra abstraction layer
- **When to use**: 
  - Multiple data sources (SQL + NoSQL)
  - Complex queries needing encapsulation
  - Want to hide EF Core from domain layer
- **When NOT to use**: Simple CRUD with EF Core only

**Q: "Generic Repository vs Specific Repository?"**
A:
- Generic: IRepository<T> - DRY but can leak data access concerns
- Specific: IAccountRepository - More explicit, domain-focused
- **Recommendation**: Specific repositories that inherit from generic base

**Q: "How do you handle Include/navigation properties?"**
A:
- Use specification pattern for complex queries
- Or provide specific methods: GetAccountWithTransactions()
- Avoid: Exposing IQueryable (leaky abstraction)

### Implementation Pattern:
```csharp
// Generic base interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}

// Specific repository
public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<IEnumerable<Account>> GetAccountsByCustomerNameAsync(string customerName);
    Task<IEnumerable<Account>> GetHighBalanceAccountsAsync(decimal minBalance);
}

// Implementation with EF Core
public class AccountRepository : IAccountRepository
{
    private readonly BankingDbContext _context;
    
    public AccountRepository(BankingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }
    
    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }
    
    // GOTCHA: Don't return IQueryable - it's a leaky abstraction!
    public async Task<IEnumerable<Account>> GetHighBalanceAccountsAsync(decimal minBalance)
    {
        return await _context.Accounts
            .Where(a => a.Balance >= minBalance)
            .ToListAsync(); // Execute query, don't return IQueryable
    }
    
    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        // GOTCHA: Don't call SaveChanges here! Use Unit of Work
    }
    
    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        // GOTCHA: Let Unit of Work handle SaveChanges
    }
}
```

---

## Pattern 6: Unit of Work Pattern
**Use Case**: Transaction Consistency Across Multiple Operations

### Interview Questions to Expect:
**Q: "How does Unit of Work relate to database transactions?"**
A:
- UoW is an abstraction over database transaction
- Ensures all changes succeed or fail together
- EF Core DbContext IS a Unit of Work!
- Explicit UoW pattern useful when coordinating multiple DbContexts

**Q: "What about nested transactions?"**
A: CRITICAL GOTCHA!
- Most databases don't support true nested transactions
- SQL Server has savepoints (not full transactions)
- **Best practice**: Don't nest transactions
- Use TransactionScope with care (distributed transactions)

**Q: "How do you handle deadlocks?"**
A:
- Retry logic with exponential backoff
- Consistent locking order
- Keep transactions short
- Monitor with database tools
- Consider optimistic concurrency (row versioning)

### Implementation Pattern:
```csharp
public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BankingDbContext _context;
    private IDbContextTransaction? _transaction;
    
    public UnitOfWork(BankingDbContext context)
    {
        _context = context;
        Accounts = new AccountRepository(_context);
        Transactions = new TransactionRepository(_context);
    }
    
    public IAccountRepository Accounts { get; }
    public ITransactionRepository Transactions { get; }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            await _transaction?.CommitAsync()!;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync()
    {
        await _transaction?.RollbackAsync()!;
        _transaction?.Dispose();
        _transaction = null;
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

// Usage example - Transfer money between accounts
public class TransferService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<bool> TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fromAccount = await _unitOfWork.Accounts.GetByIdAsync(fromAccountId);
            var toAccount = await _unitOfWork.Accounts.GetByIdAsync(toAccountId);
            
            fromAccount.Withdraw(amount);
            toAccount.Deposit(amount);
            
            await _unitOfWork.Accounts.UpdateAsync(fromAccount);
            await _unitOfWork.Accounts.UpdateAsync(toAccount);
            
            // Create transaction records
            var transaction1 = new Transaction(fromAccountId, TransactionType.Withdrawal, amount);
            var transaction2 = new Transaction(toAccountId, TransactionType.Deposit, amount);
            
            await _unitOfWork.Transactions.AddAsync(transaction1);
            await _unitOfWork.Transactions.AddAsync(transaction2);
            
            await _unitOfWork.CommitTransactionAsync();
            
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

---

## Pattern 7: Singleton Pattern
**Use Case**: Configuration Manager

### Interview Questions to Expect:
**Q: "Why is Singleton considered an anti-pattern?"**
A: IMPORTANT!
- Global state makes testing hard
- Hidden dependencies
- Thread-safety issues
- Doesn't work in distributed systems
- Makes code tightly coupled
- **Modern alternative**: Use DI with singleton lifetime

**Q: "How do you make Singleton thread-safe?"**
A:
- Lazy<T> (.NET built-in, thread-safe)
- Double-check locking (complex, error-prone)
- Static constructor (simple, thread-safe)
- **Best**: Don't use Singleton, use DI

**Q: "What about Singleton in cloud/distributed systems?"**
A:
- Singleton only works within one process
- In containers/microservices, each instance has own "singleton"
- Use Redis/distributed cache for shared state
- Consider stateless design instead

### Implementation Pattern:
```csharp
// BAD: Traditional Singleton (don't do this in modern C#)
public sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance = 
        new Lazy<ConfigurationManager>(() => new ConfigurationManager());
    
    private ConfigurationManager()
    {
        // Load configuration
    }
    
    public static ConfigurationManager Instance => _instance.Value;
    
    public string GetConnectionString() => "...";
}

// GOOD: Use DI with singleton lifetime instead
public interface IConfigurationService
{
    string GetConnectionString();
    decimal GetMaxTransactionAmount();
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")!;
    }
    
    public decimal GetMaxTransactionAmount()
    {
        return _configuration.GetValue<decimal>("MaxTransactionAmount");
    }
}

// DI Registration
services.AddSingleton<IConfigurationService, ConfigurationService>();
```

---

## Pattern 8: Adapter Pattern
**Use Case**: Third-Party Payment Gateway Integration

### Interview Questions to Expect:
**Q: "What's the difference between Adapter and Facade?"**
A:
- **Adapter**: Makes one interface work with another (interface conversion)
- **Facade**: Simplifies complex subsystem (unified interface)
- **Example**: Adapter = plug converter, Facade = universal remote

**Q: "Object Adapter vs Class Adapter?"**
A:
- **Object Adapter**: Uses composition (preferred in C#)
- **Class Adapter**: Uses multiple inheritance (not in C#)

**Q: "When would you use Adapter?"**
A:
- Integrating third-party libraries
- Legacy code integration
- Multiple payment gateways
- Can't modify existing interfaces

### Implementation Pattern:
```csharp
// Our domain interface
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, string accountNumber);
    Task<bool> RefundAsync(string transactionId);
}

// Third-party gateway (we don't control this)
public class StripePaymentService
{
    public async Task<StripeResponse> Charge(int amountInCents, string token)
    {
        // Stripe's API
        return new StripeResponse();
    }
    
    public async Task<bool> CreateRefund(string chargeId)
    {
        // Stripe's refund API
        return true;
    }
}

// Adapter
public class StripePaymentAdapter : IPaymentGateway
{
    private readonly StripePaymentService _stripeService;
    
    public StripePaymentAdapter(StripePaymentService stripeService)
    {
        _stripeService = stripeService;
    }
    
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string accountNumber)
    {
        // GOTCHA: Converting between different units (dollars to cents)
        int amountInCents = (int)(amount * 100);
        
        var stripeResponse = await _stripeService.Charge(amountInCents, accountNumber);
        
        // Adapt response to our domain model
        return new PaymentResult
        {
            Success = stripeResponse.Success,
            TransactionId = stripeResponse.ChargeId
        };
    }
    
    public async Task<bool> RefundAsync(string transactionId)
    {
        return await _stripeService.CreateRefund(transactionId);
    }
}

// Another adapter for different gateway
public class PayPalPaymentAdapter : IPaymentGateway
{
    // Similar implementation for PayPal
}

// DI Configuration
services.AddScoped<IPaymentGateway>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var gateway = config["PaymentGateway"];
    
    return gateway switch
    {
        "Stripe" => new StripePaymentAdapter(sp.GetRequiredService<StripePaymentService>()),
        "PayPal" => new PayPalPaymentAdapter(sp.GetRequiredService<PayPalService>()),
        _ => throw new InvalidOperationException("Unknown payment gateway")
    };
});
```

---

## Pattern 9: Command Pattern
**Use Case**: Transaction Commands with Undo/Audit Trail

### Interview Questions to Expect:
**Q: "How do you handle undo in financial systems?"**
A: CRITICAL!
- Financial transactions typically can't be "undone"
- Instead, create compensating transaction (reversal)
- Maintain audit trail of all commands
- Use Command pattern for transaction log
- Consider Event Sourcing for full history

**Q: "Command vs Command Query Responsibility Segregation (CQRS)?"**
A:
- Command pattern: Encapsulates request as object
- CQRS: Architectural pattern separating reads/writes
- CQRS often uses Command pattern for write operations

**Q: "How do you handle async commands?"**
A:
- Queue commands (RabbitMQ, Azure Service Bus)
- Use async/await throughout
- Handle timeouts and retries
- Implement idempotency (very important!)

### Implementation Pattern:
```csharp
// Command interface
public interface ICommand
{
    Task ExecuteAsync();
    Task UndoAsync(); // In banking, this creates reversing entry
    string CommandType { get; }
    DateTime Timestamp { get; }
}

// Concrete command
public class WithdrawCommand : ICommand
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Guid _accountId;
    private readonly decimal _amount;
    private Guid _transactionId;
    
    public WithdrawCommand(IUnitOfWork unitOfWork, Guid accountId, decimal amount)
    {
        _unitOfWork = unitOfWork;
        _accountId = accountId;
        _amount = amount;
        Timestamp = DateTime.UtcNow;
    }
    
    public string CommandType => "Withdraw";
    public DateTime Timestamp { get; }
    
    public async Task ExecuteAsync()
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(_accountId);
        account.Withdraw(_amount);
        
        var transaction = new Transaction(_accountId, TransactionType.Withdrawal, _amount);
        _transactionId = transaction.Id;
        
        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task UndoAsync()
    {
        // IMPORTANT: In banking, we don't "undo", we create reversing entry
        var account = await _unitOfWork.Accounts.GetByIdAsync(_accountId);
        account.Deposit(_amount);
        
        var reversingTransaction = new Transaction(_accountId, TransactionType.Deposit, _amount)
        {
            Description = $"Reversal of transaction {_transactionId}",
            Status = TransactionStatus.Reversed
        };
        
        await _unitOfWork.Transactions.AddAsync(reversingTransaction);
        await _unitOfWork.SaveChangesAsync();
    }
}

// Command invoker
public class TransactionCommandInvoker
{
    private readonly Stack<ICommand> _commandHistory = new();
    
    public async Task ExecuteCommandAsync(ICommand command)
    {
        await command.ExecuteAsync();
        _commandHistory.Push(command);
        
        // PRODUCTION: Persist command to audit log database
    }
    
    public async Task UndoLastCommandAsync()
    {
        if (_commandHistory.Count > 0)
        {
            var command = _commandHistory.Pop();
            await command.UndoAsync();
        }
    }
    
    // STAFF ENGINEER: Implement command queue for async processing
    public async Task QueueCommandAsync(ICommand command)
    {
        // Add to message queue (RabbitMQ, Azure Service Bus, etc.)
        // Implement idempotency token to prevent duplicate processing
    }
}
```

---

## Summary: Key Talking Points for Staff Engineer Interview

### When to Use Each Pattern:
1. **Strategy**: Multiple algorithms, runtime selection needed
2. **Chain of Responsibility**: Request processing through multiple handlers
3. **Observer**: One-to-many event notification
4. **Decorator**: Adding responsibilities dynamically
5. **Repository/UoW**: Data access abstraction and transaction management
6. **Singleton**: DON'T - use DI instead
7. **Adapter**: Interface compatibility with third-party systems
8. **Command**: Encapsulate requests, audit trail, queue operations

### Critical Banking Concepts:
- **Idempotency**: Same operation multiple times = same result
- **Audit Trail**: Every transaction must be traceable
- **Consistency**: ACID properties in databases
- **Concurrency**: Handle race conditions with optimistic/pessimistic locking
- **Compensation**: Can't undo financial transactions, must reverse
- **Thread Safety**: Critical in multi-user financial systems

### Staff Engineer Expectations:
- Explain trade-offs, not just implementation
- Discuss scalability and performance
- Consider distributed systems scenarios
- Think about monitoring and observability
- Know when NOT to use patterns
- Understand modern alternatives (DI, middleware, CQRS, Event Sourcing)

Good luck with your interview! 🚀
