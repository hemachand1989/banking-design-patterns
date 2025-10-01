  };
  
  return (
    <div>
      <h2>Balance: ${state.balance}</h2>
      <button onClick={() => withdraw(100)}>Withdraw $100</button>
      <button onClick={undo}>Undo Last Transaction</button>
      
      <h3>Transaction History (Audit Trail)</h3>
      {state.history.map((cmd, i) => (
        <div key={i}>
          {cmd.type}: ${cmd.payload.amount} at {cmd.payload.timestamp.toISOString()}
        </div>
      ))}
    </div>
  );
};
```

**Interview Gotcha**: "Why use Command pattern in React?"
- **Audit trail**: All state changes tracked
- **Undo/Redo**: Easy to implement with command history
- **Time travel debugging**: Redux DevTools leverage this
- **Testability**: Commands are pure functions
- **Serialization**: Commands can be saved/replayed

---

### 5. Factory Pattern → Factory Functions / Render Props
**Backend (C#)**:
```csharp
public class AccountFactory {
    public IAccount CreateAccount(AccountType type) { /* ... */ }
}
```

**Frontend (React)**:
```tsx
// Factory function for component creation
const createAccountComponent = (accountType: AccountType) => {
  switch (accountType) {
    case 'savings':
      return SavingsAccountView;
    case 'checking':
      return CheckingAccountView;
    case 'premium':
      return PremiumAccountView;
    default:
      throw new Error('Unknown account type');
  }
};

// Usage
const AccountDashboard: React.FC<{ accountType: AccountType }> = ({ accountType }) => {
  const AccountComponent = createAccountComponent(accountType);
  return <AccountComponent />;
};

// More React-way: Render prop pattern
interface AccountFactoryProps {
  accountType: AccountType;
  render: (account: Account) => ReactElement;
}

const AccountFactory: React.FC<AccountFactoryProps> = ({ accountType, render }) => {
  const account = useMemo(() => {
    // Factory logic to create account object
    return createAccountObject(accountType);
  }, [accountType]);
  
  return render(account);
};

// Usage
<AccountFactory 
  accountType="savings" 
  render={(account) => <AccountDetails account={account} />} 
/>
```

---

### 6. Repository Pattern → Custom Hooks for Data Fetching
**Backend (C#)**:
```csharp
public interface IAccountRepository {
    Task<Account> GetByIdAsync(Guid id);
    Task<List<Account>> GetAllAsync();
}
```

**Frontend (React)**:
```tsx
// Repository pattern as custom hook
const useAccountRepository = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  
  const getById = async (id: string): Promise<Account | null> => {
    setLoading(true);
    try {
      const response = await fetch(`/api/accounts/${id}`);
      const account = await response.json();
      return account;
    } catch (err) {
      setError(err as Error);
      return null;
    } finally {
      setLoading(false);
    }
  };
  
  const getAll = async (): Promise<Account[]> => {
    setLoading(true);
    try {
      const response = await fetch('/api/accounts');
      return await response.json();
    } catch (err) {
      setError(err as Error);
      return [];
    } finally {
      setLoading(false);
    }
  };
  
  return { getById, getAll, loading, error };
};

// Usage in component
const AccountList: React.FC = () => {
  const repository = useAccountRepository();
  const [accounts, setAccounts] = useState<Account[]>([]);
  
  useEffect(() => {
    repository.getAll().then(setAccounts);
  }, []);
  
  if (repository.loading) return <Spinner />;
  if (repository.error) return <Error message={repository.error.message} />;
  
  return (
    <ul>
      {accounts.map(account => (
        <li key={account.id}>{account.name}</li>
      ))}
    </ul>
  );
};

// Modern alternative: React Query (similar to Repository + Caching)
const useAccounts = () => {
  return useQuery({
    queryKey: ['accounts'],
    queryFn: async () => {
      const response = await fetch('/api/accounts');
      return response.json();
    }
  });
};
```

**Interview Question**: "Custom hooks vs React Query/SWR?"
- **Custom hooks**: Full control, but need to handle caching, deduplication manually
- **React Query**: Built-in caching, automatic refetching, optimistic updates
- **For banking apps**: React Query recommended for data fetching layer
- **Pattern still applies**: Abstraction over data access

---

### 7. Adapter Pattern → API Adapters / Type Conversion
**Backend (C#)**:
```csharp
public class StripeAdapter : IPaymentGateway {
    private StripeService _stripe;
    public PaymentResult Process(decimal amount) {
        // Adapt dollars to cents
        int cents = (int)(amount * 100);
        return _stripe.Charge(cents);
    }
}
```

**Frontend (React)**:
```tsx
// Adapter for different API formats
interface BankingAPI {
  fetchBalance(accountId: string): Promise<number>;
  makeTransaction(transaction: Transaction): Promise<TransactionResult>;
}

// Legacy API adapter
class LegacyAPIAdapter implements BankingAPI {
  async fetchBalance(accountId: string): Promise<number> {
    // Legacy API returns { account_id, bal } 
    const response = await fetch(`/legacy/account/${accountId}`);
    const data = await response.json();
    // Adapt to our format
    return data.bal / 100; // Cents to dollars
  }
  
  async makeTransaction(transaction: Transaction): Promise<TransactionResult> {
    // Adapt our format to legacy format
    const legacyPayload = {
      acct_id: transaction.accountId,
      amt: transaction.amount * 100, // Dollars to cents
      trans_type: transaction.type.toUpperCase()
    };
    
    const response = await fetch('/legacy/transaction', {
      method: 'POST',
      body: JSON.stringify(legacyPayload)
    });
    
    const result = await response.json();
    
    // Adapt response back
    return {
      success: result.status === 'OK',
      transactionId: result.trans_id
    };
  }
}

// New API adapter
class ModernAPIAdapter implements BankingAPI {
  async fetchBalance(accountId: string): Promise<number> {
    const response = await fetch(`/api/v2/accounts/${accountId}/balance`);
    const data = await response.json();
    return data.balance;
  }
  
  async makeTransaction(transaction: Transaction): Promise<TransactionResult> {
    const response = await fetch('/api/v2/transactions', {
      method: 'POST',
      body: JSON.stringify(transaction)
    });
    return await response.json();
  }
}

// Hook using adapter
const useBankingAPI = (): BankingAPI => {
  // Select adapter based on configuration
  const apiVersion = useConfig().apiVersion;
  
  return useMemo(() => {
    return apiVersion === 'legacy' 
      ? new LegacyAPIAdapter() 
      : new ModernAPIAdapter();
  }, [apiVersion]);
};

// Component doesn't care about API version
const AccountBalance: React.FC<{ accountId: string }> = ({ accountId }) => {
  const api = useBankingAPI();
  const [balance, setBalance] = useState<number>(0);
  
  useEffect(() => {
    api.fetchBalance(accountId).then(setBalance);
  }, [accountId, api]);
  
  return <div>Balance: ${balance.toFixed(2)}</div>;
};
```

---

## Common Interview Questions: React + Patterns

### Q1: "How do you implement state management patterns in React?"
**A**: 
- **Local state**: useState for component-level
- **Lifted state**: Pass down props (prop drilling)
- **Context**: For cross-cutting concerns (auth, theme)
- **Redux/Zustand**: For complex global state
- **React Query**: For server state (caching, sync)
- **Pattern**: Separate server state from client state

### Q2: "How do you handle async operations in React?"
**A**:
```tsx
// Command pattern with async
const useAsyncCommand = <T,>(command: () => Promise<T>) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [data, setData] = useState<T | null>(null);
  
  const execute = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await command();
      setData(result);
      return result;
    } catch (err) {
      setError(err as Error);
      throw err;
    } finally {
      setLoading(false);
    }
  };
  
  return { execute, loading, error, data };
};

// Usage
const { execute, loading } = useAsyncCommand(async () => {
  return await api.makeTransaction(transaction);
});
```

### Q3: "How do you implement undo/redo in React?"
**A**:
```tsx
// Command pattern with history
const useUndoable = <T,>(initialState: T) => {
  const [history, setHistory] = useState<T[]>([initialState]);
  const [index, setIndex] = useState(0);
  
  const setState = (newState: T) => {
    const newHistory = history.slice(0, index + 1);
    setHistory([...newHistory, newState]);
    setIndex(newHistory.length);
  };
  
  const undo = () => {
    if (index > 0) setIndex(index - 1);
  };
  
  const redo = () => {
    if (index < history.length - 1) setIndex(index + 1);
  };
  
  return {
    state: history[index],
    setState,
    undo,
    redo,
    canUndo: index > 0,
    canRedo: index < history.length - 1
  };
};
```

### Q4: "How do you prevent memory leaks in React?"
**A**:
- **Always cleanup in useEffect**
- **Unsubscribe from observables**
- **Cancel pending promises on unmount**
- **Use AbortController for fetch**
```tsx
useEffect(() => {
  const controller = new AbortController();
  
  fetch('/api/data', { signal: controller.signal })
    .then(response => response.json())
    .then(setData);
  
  return () => {
    controller.abort(); // Cleanup!
  };
}, []);
```

### Q5: "How do you optimize React performance with patterns?"
**A**:
- **Memoization**: useMemo, useCallback, React.memo
- **Code splitting**: Lazy loading with factory pattern
- **Virtual scrolling**: For large lists
- **Debouncing/Throttling**: For expensive operations
```tsx
// Factory with lazy loading
const createAccountComponent = (type: AccountType) => {
  switch (type) {
    case 'savings':
      return lazy(() => import('./SavingsAccount'));
    case 'checking':
      return lazy(() => import('./CheckingAccount'));
  }
};
```

---

## Key Takeaways for Fullstack Interview

### Backend (C# .NET Core):
- Focus on SOLID principles
- Thread safety and concurrency
- Database transactions and consistency
- API design and versioning

### Frontend (React):
- **Hooks** are the React equivalent of many patterns
- **Context** for Observer-like behavior
- **useReducer** for Command pattern
- **Custom hooks** for reusable logic (Strategy)
- **React Query** for data fetching (Repository)

### Common Ground:
- **Separation of concerns** in both
- **Testability** is key
- **Type safety** (C# + TypeScript)
- **Error handling** patterns
- **Async/await** throughout stack

### Interview Gold:
- Mention how patterns translate frontend/backend
- Discuss **API design** that works well with React
- Talk about **state management** strategy
- Consider **real-time** updates (SignalR + WebSockets)
- Think about **optimistic UI** updates

---

## Sample Fullstack Architecture

```
┌─────────────────┐
│  React Frontend │
│  - Custom Hooks │ ← Strategy, Repository patterns
│  - Context API  │ ← Observer pattern
│  - useReducer   │ ← Command pattern
└────────┬────────┘
         │
    HTTP/SignalR
         │
┌────────▼────────┐
│  ASP.NET Core   │
│  - Controllers  │
│  - Mediator     │ ← Command pattern (MediatR)
│  - Services     │ ← Strategy, Factory patterns
└────────┬────────┘
         │
┌────────▼────────┐
│  Domain Layer   │
│  - Aggregates   │
│  - Entities     │
│  - Patterns     │ ← Strategy, Chain, Observer
└────────┬────────┘
         │
┌────────▼────────┐
│  Data Layer     │
│  - Repository   │ ← Repository pattern
│  - UnitOfWork   │ ← UoW pattern
│  - EF Core      │
└─────────────────┘
```

---

## Best Practices: React + .NET Core

### 1. Type Safety End-to-End
```typescript
// Share types between frontend and backend
export interface Account {
  id: string;
  accountNumber: string;
  balance: number;
  accountType: 'savings' | 'checking' | 'premium';
}

// Use same DTO on backend (C#)
public class AccountDto {
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public AccountType AccountType { get; set; }
}
```

### 2. Error Handling Pattern
```tsx
// Frontend hook
const useApiCall = <T,>(apiCall: () => Promise<T>) => {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<ApiError | null>(null);
  
  const execute = async () => {
    try {
      const result = await apiCall();
      setData(result);
    } catch (err) {
      // Handle API error structure from backend
      if (err instanceof Response) {
        const errorData = await err.json();
        setError({
          message: errorData.message,
          code: errorData.code,
          validationErrors: errorData.errors
        });
      }
    }
  };
  
  return { data, error, execute };
};
```

### 3. Real-time Updates with SignalR
```tsx
// Observer pattern with SignalR
const useAccountBalanceUpdates = (accountId: string) => {
  const [balance, setBalance] = useState(0);
  
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/accountHub')
      .build();
    
    connection.on('BalanceUpdated', (data) => {
      if (data.accountId === accountId) {
        setBalance(data.newBalance);
      }
    });
    
    connection.start();
    
    return () => {
      connection.stop(); // Cleanup!
    };
  }, [accountId]);
  
  return balance;
};
```

---

**Remember**: In a fullstack interview, show how patterns work across the entire stack!

Good luck! 🚀
