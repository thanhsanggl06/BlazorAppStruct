# ?? Repository Pattern - Luôn dùng UnitOfWork

## ? Thi?t k? m?i (Chu?n)

### Nguyên t?c:
- **Repository LUÔN nh?n UnitOfWork** qua constructor
- **Service quy?t ð?nh** có dùng transaction hay không
- **Ðõn gi?n, r? ràng** - không có dual-mode ph?c t?p

## ??? Architecture

```
Repository (LUÔN dùng UnitOfWork)
    ?
UnitOfWork
    ??? Connection (lazy-created)
    ??? Transaction (null n?u chýa BeginTransaction)
```

### Repository Implementation

```csharp
public class TodoRepository : ITodoRepository
{
    private readonly IUnitOfWork _unitOfWork;

    // CH? có 1 constructor - LUÔN nh?n UnitOfWork
    public TodoRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Title, IsDone, CreatedAt FROM TodoItems WHERE Id = @Id";
        
        // LUÔN dùng _unitOfWork.Connection và _unitOfWork.Transaction
        var result = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(
            new CommandDefinition(sql, new { Id = id }, _unitOfWork.Transaction, cancellationToken: ct)
        );
        
        return result;
    }
    
    // Các methods khác týõng t?...
}
```

**Ð?c ði?m:**
- ? **Ðõn gi?n** - ch? 1 constructor
- ? **R? ràng** - luôn dùng UnitOfWork
- ? **Không có conditional logic** - không c?n ki?m tra null
- ? **Service quy?t ð?nh transaction** - không ph?i repository

## ?? Cách s? d?ng

### 1?? Query ðõn gi?n KHÔNG C?N Transaction

```csharp
public class TodoQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoRepository _repository;

    public async Task<TodoItemDto?> GetTodoByIdAsync(int id)
    {
        // KHÔNG g?i BeginTransaction()
        // ? Repository dùng connection KHÔNG có transaction
        // ? M?i operation t? commit
        
        return await _repository.GetByIdAsync(id);
    }
}
```

**Flow:**
```
1. Repository call GetByIdAsync
2. UnitOfWork.Connection ðý?c s? d?ng
3. UnitOfWork.Transaction = null (chýa begin)
4. Query execute WITHOUT transaction
5. Auto-commit sau query
```

### 2?? Multiple Operations C?N Transaction

```csharp
public async Task<int> CreateMultipleTodosAsync(List<string> titles)
{
    try
    {
        // G?I BeginTransaction() trý?c khi dùng Repository
        _unitOfWork.BeginTransaction();

        foreach (var title in titles)
        {
            await _repository.CreateAsync(title);
        }

        _unitOfWork.Commit(); // Commit t?t c?
        return titles.Count;
    }
    catch
    {
        _unitOfWork.Rollback(); // Rollback t?t c?
        throw;
    }
}
```

**Flow:**
```
1. Service g?i BeginTransaction()
2. UnitOfWork.Transaction ðý?c t?o
3. Repository calls dùng transaction
4. Service g?i Commit/Rollback
```

### 3?? Custom Isolation Level

```csharp
public async Task<IReadOnlyList<TodoItemDto>> GetTodosSerializableAsync()
{
    try
    {
        // Serializable isolation ð? tránh phantom reads
        _unitOfWork.BeginTransaction(IsolationLevel.Serializable);

        var todos = await _repository.GetAllAsync();

        _unitOfWork.Commit();
        return todos;
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

### 4?? Dirty Read (ReadUncommitted)

```csharp
public async Task<int> GetApproximateCountAsync()
{
    try
    {
        // ReadUncommitted: Nhanh nhýng dirty read
        _unitOfWork.BeginTransaction(IsolationLevel.ReadUncommitted);

        var count = await _repository.CountAsync();

        _unitOfWork.Commit();
        return count;
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

## ?? Use Cases Chi Ti?t

### Use Case 1: Simple CRUD (No Transaction)

```csharp
// ? GOOD: Single operation, no transaction needed
public async Task<TodoItemDto?> GetTodoAsync(int id)
{
    return await _repository.GetByIdAsync(id);
    // Auto-commit
}

public async Task<int> CreateTodoAsync(string title)
{
    return await _repository.CreateAsync(title);
    // Auto-commit
}

public async Task<bool> UpdateTodoAsync(int id, string title, bool isDone)
{
    return await _repository.UpdateAsync(id, title, isDone);
    // Auto-commit
}
```

**Khi nào dùng:**
- ? Single database operation
- ? No need for atomicity across operations
- ? Simple queries

### Use Case 2: Multi-step Operations (With Transaction)

```csharp
// ? GOOD: Multiple operations need atomicity
public async Task<(int Created, int Updated)> BatchUpdateAsync(...)
{
    try
    {
        _unitOfWork.BeginTransaction();

        // Step 1: Create new todos
        var created = 0;
        foreach (var title in newTitles)
        {
            await _repository.CreateAsync(title);
            created++;
        }

        // Step 2: Update existing todos
        var updated = 0;
        foreach (var id in idsToUpdate)
        {
            await _repository.UpdateAsync(id, "Updated", true);
            updated++;
        }

        _unitOfWork.Commit(); // All or nothing
        return (created, updated);
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

**Khi nào dùng:**
- ? Multiple related operations
- ? Need atomicity (all or nothing)
- ? Complex business logic

### Use Case 3: Read Consistency (Transaction for Queries)

```csharp
// ? GOOD: Read multiple data points consistently
public async Task<TodoStatistics> GetStatisticsAsync()
{
    try
    {
        // Begin transaction ð? ð?c snapshot t?i cùng th?i ði?m
        _unitOfWork.BeginTransaction(IsolationLevel.ReadCommitted);

        var todos = await _repository.GetAllAsync();
        var completedCount = todos.Count(t => t.IsDone);
        var pendingCount = todos.Count - completedCount;

        _unitOfWork.Commit();

        return new TodoStatistics
        {
            Total = todos.Count,
            Completed = completedCount,
            Pending = pendingCount
        };
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

**Khi nào dùng:**
- ? Multiple queries c?n consistent snapshot
- ? Reports/statistics
- ? Avoid inconsistent data

### Use Case 4: Validation Before Execute

```csharp
// ? GOOD: Two-phase pattern
public async Task<int> BulkDeleteAsync(List<int> ids)
{
    try
    {
        _unitOfWork.BeginTransaction();

        // Phase 1: Validate ALL
        foreach (var id in ids)
        {
            var todo = await _repository.GetByIdAsync(id);
            if (todo == null)
                throw new InvalidOperationException($"Todo {id} not found");
            if (!todo.IsDone)
                throw new InvalidOperationException($"Todo {id} not completed");
        }

        // Phase 2: Execute ALL
        var deletedCount = 0;
        foreach (var id in ids)
        {
            await _repository.DeleteAsync(id);
            deletedCount++;
        }

        _unitOfWork.Commit();
        return deletedCount;
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

## ?? Comparison: Old vs New

| Aspect | Old (Dual Constructor) | New (UnitOfWork Only) |
|--------|------------------------|----------------------|
| **Constructors** | 2 (Factory + UnitOfWork) | 1 (UnitOfWork only) |
| **Complexity** | High (conditional logic) | Low (straightforward) |
| **Transaction Control** | Repository decides | **Service decides** ? |
| **Connection Management** | Manual dispose needed | UnitOfWork handles |
| **Testability** | Medium | **High** ? |
| **Code Clarity** | Confusing | **Clear** ? |

## ?? Key Principles

### 1. Single Responsibility
```csharp
// Repository: CH? lo data access
public async Task<TodoItemDto?> GetByIdAsync(int id)
{
    return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(...);
}

// Service: Lo business logic + transaction control
public async Task DoSomething()
{
    _unitOfWork.BeginTransaction(); // Service quy?t ð?nh
    await _repository.GetByIdAsync(1);
    _unitOfWork.Commit();
}
```

### 2. Dependency Inversion
```csharp
// Repository depends on abstraction (IUnitOfWork)
public TodoRepository(IUnitOfWork unitOfWork) { }

// NOT on concrete implementation
// public TodoRepository(UnitOfWork unitOfWork) { } ?
```

### 3. Service Controls Transaction Scope
```csharp
// Service decides transaction boundaries
public async Task Method1() 
{
    // No transaction - simple query
    await _repository.GetByIdAsync(1);
}

public async Task Method2()
{
    // With transaction - complex operation
    _unitOfWork.BeginTransaction();
    await _repository.CreateAsync("Todo");
    _unitOfWork.Commit();
}
```

## ?? Common Mistakes

### ? Mistake 1: Quên Dispose UnitOfWork
```csharp
// BAD
var uow = new UnitOfWork(factory);
uow.BeginTransaction();
// ... forgot to dispose
```

**Fix:** DI container t? ð?ng dispose (Scoped lifetime)

### ? Mistake 2: Multiple BeginTransaction
```csharp
// BAD
_unitOfWork.BeginTransaction();
_unitOfWork.BeginTransaction(); // Exception!
```

**Fix:** Ch? g?i BeginTransaction() m?t l?n per scope

### ? Mistake 3: Không Commit/Rollback
```csharp
// BAD
_unitOfWork.BeginTransaction();
await _repository.CreateAsync("Todo");
// Forgot commit ? auto rollback on dispose
```

**Fix:** Luôn Commit ho?c Rollback explicitly

## ?? Testing

### Mock UnitOfWork
```csharp
var mockUow = new Mock<IUnitOfWork>();
var mockConn = new Mock<IDbConnection>();
var mockTran = new Mock<IDbTransaction>();

mockUow.Setup(u => u.Connection).Returns(mockConn.Object);
mockUow.Setup(u => u.Transaction).Returns(mockTran.Object);

var repository = new TodoRepository(mockUow.Object);
```

### Verify Transaction Calls
```csharp
mockUow.Verify(u => u.BeginTransaction(), Times.Once);
mockUow.Verify(u => u.Commit(), Times.Once);
mockUow.Verify(u => u.Rollback(), Times.Never);
```

## ?? Summary

? **Repository luôn nh?n UnitOfWork** - ðõn gi?n, r? ràng  
? **Service quy?t ð?nh transaction** - linh ho?t, ki?m soát t?t  
? **Không c?n dual-mode** - ít code, ít bug  
? **Testability cao** - d? mock  
? **Separation of concerns** - repository ch? lo data access  

**Bottom line:** Service controls WHEN to use transaction, Repository just executes queries.

---

**Version:** 3.0 - UnitOfWork Only Pattern  
**Updated:** 2024
