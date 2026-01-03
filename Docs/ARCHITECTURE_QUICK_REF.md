# ??? Dapper Enterprise Architecture - Quick Reference

## C?u trúc Layers

```
???????????????????????????????????????????
?         API Layer (Server)              ?
?  TodoTransactionController.cs           ?
?  - Nh?n HTTP requests                   ?
?  - Return responses                     ?
???????????????????????????????????????????
                 ?
                 ?
???????????????????????????????????????????
?       Service Layer (Services)          ?
?  TodoTransactionService.cs              ?
?  - Business logic                       ?
?  - Transaction orchestration            ?
?  - Validation rules                     ?
???????????????????????????????????????????
                 ?
                 ?
???????????????????????????????????????????
?    Repository Layer (Data/Dapper)       ?
?  TodoRepository.cs                      ?
?  - Data access logic                    ?
?  - SQL queries                          ?
?  - Mapping                              ?
???????????????????????????????????????????
                 ?
                 ?
???????????????????????????????????????????
?  Infrastructure (Data/Dapper)           ?
?  - IDbConnectionFactory: T?o connection ?
?  - IUnitOfWork: Qu?n l? transaction     ?
???????????????????????????????????????????
                 ?
                 ?
          ????????????????
          ?   Database   ?
          ?  SQL Server  ?
          ????????????????
```

## Request Flow

```
1. Client Request
   POST /api/todotransaction/create-multiple
   Body: ["Todo 1", "Todo 2", "Todo 3"]
        ?
        ?
2. Controller
   TodoTransactionController.CreateMultipleTodos()
   - Validate request
   - Call service
        ?
        ?
3. Service (v?i UnitOfWork)
   using var uow = new UnitOfWork(factory)
   uow.BeginTransaction()
        ?
        ?
4. Repository (nhi?u l?n)
   repository.CreateAsync("Todo 1") ??
   repository.CreateAsync("Todo 2")  ? Cùng transaction
   repository.CreateAsync("Todo 3") ??
        ?
        ?
5. Commit/Rollback
   uow.Commit() ho?c uow.Rollback()
        ?
        ?
6. Response
   { success: true, todoIds: [1,2,3] }
```

## Component Responsibilities

### 1. IDbConnectionFactory
```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
```
**Nhi?m v?:**
- T?o và m? database connection
- Singleton lifetime (stateless)
- Connection pooling t? ð?ng (b?i SqlConnection)

### 2. IUnitOfWork
```csharp
public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    void BeginTransaction();
    void Commit();
    void Rollback();
}
```
**Nhi?m v?:**
- Qu?n l? 1 connection cho multiple operations
- Qu?n l? transaction lifecycle
- Scoped lifetime (1 instance/request)
- Auto rollback khi dispose n?u chýa commit

### 3. Repository
```csharp
public class TodoRepository : ITodoRepository
{
    public TodoRepository(IDbConnectionFactory factory) { }
    public TodoRepository(IUnitOfWork unitOfWork) { }
}
```
**Nhi?m v?:**
- Encapsulate data access
- Execute SQL v?i Dapper
- Map results to DTOs
- Support 2 modes: Standalone & UnitOfWork

### 4. Service
```csharp
public class TodoTransactionService
{
    public async Task CreateMultipleTodosAsync(...)
    {
        using var uow = new UnitOfWork(factory);
        var repo = new TodoRepository(uow);
        // Business logic v?i transaction
    }
}
```
**Nhi?m v?:**
- Implement business logic
- Orchestrate multiple repository calls
- Manage transactions
- Handle errors và rollback

## Dependency Injection Setup

```csharp
// Program.cs
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoTransactionService, TodoTransactionService>();
```

**Lifetimes:**
- **Singleton**: ConnectionFactory (no state)
- **Scoped**: UnitOfWork, Repository, Service (per-request)

## Transaction Patterns

### Pattern 1: Single Operation (No Transaction)
```csharp
var repository = new TodoRepository(connectionFactory);
var todo = await repository.GetByIdAsync(1);
// Auto open/close connection
```

### Pattern 2: Multiple Operations (With Transaction)
```csharp
using var uow = new UnitOfWork(connectionFactory);
var repository = new TodoRepository(uow);

try
{
    uow.BeginTransaction();
    
    await repository.CreateAsync("Todo 1");
    await repository.CreateAsync("Todo 2");
    await repository.CreateAsync("Todo 3");
    
    uow.Commit(); // All or nothing
}
catch
{
    uow.Rollback();
    throw;
}
```

### Pattern 3: Validation + Execute
```csharp
uow.BeginTransaction();

// Phase 1: Validate all
foreach (var id in ids)
{
    var item = await repo.GetByIdAsync(id);
    if (item == null) throw new Exception();
}

// Phase 2: Execute all
foreach (var id in ids)
{
    await repo.DeleteAsync(id);
}

uow.Commit();
```

## Key Design Decisions

### ? T?i sao tách Factory và UnitOfWork?

**Factory (Singleton):**
- T?o connection m?i m?i l?n
- Không gi? state
- Thread-safe

**UnitOfWork (Scoped):**
- Gi? 1 connection/transaction cho lifetime c?a scope
- Có state (connection, transaction)
- Tied to request lifetime

### ? T?i sao Repository có 2 constructors?

**Flexibility:**
```csharp
// Standalone mode: Simple CRUD
new TodoRepository(connectionFactory)

// UnitOfWork mode: Complex transactions
new TodoRepository(unitOfWork)
```

### ? T?i sao không dùng RepositoryBase?

**Answer:** Trong th?c t?:
- M?i entity có queries khác nhau
- Generic base class thý?ng không cover h?t use cases
- Prefer composition over inheritance
- Easier to test và maintain

## Common Gotchas

### ? Quên Dispose UnitOfWork
```csharp
// BAD
var uow = new UnitOfWork(factory);
uow.BeginTransaction();
// ... forgot to dispose
```

```csharp
// GOOD
using var uow = new UnitOfWork(factory);
uow.BeginTransaction();
// Auto dispose at end of scope
```

### ? Không Handle Exceptions
```csharp
// BAD
uow.BeginTransaction();
await repo.CreateAsync("Todo");
uow.Commit();
// Exception ? connection leaked
```

```csharp
// GOOD
try
{
    uow.BeginTransaction();
    await repo.CreateAsync("Todo");
    uow.Commit();
}
catch
{
    uow.Rollback();
    throw;
}
```

### ? Long-running Transactions
```csharp
// BAD
uow.BeginTransaction();
await SendEmail(); // Blocking operation trong transaction
await repo.CreateAsync("Todo");
uow.Commit();
```

```csharp
// GOOD
uow.BeginTransaction();
await repo.CreateAsync("Todo");
uow.Commit();
await SendEmail(); // Sau transaction
```

## Testing Strategy

### Unit Test Repository
```csharp
// Mock IDbConnectionFactory
var mockFactory = new Mock<IDbConnectionFactory>();
var repo = new TodoRepository(mockFactory.Object);
```

### Integration Test Service
```csharp
// Use real database
var factory = new SqlConnectionFactory(config);
var service = new TodoTransactionService(factory, logger);

// Test rollback behavior
await Assert.ThrowsAsync<Exception>(() => 
    service.CreateMultipleTodosAsync(invalidData)
);
```

## Performance Metrics

| Operation | Dapper | EF Core | Speedup |
|-----------|--------|---------|---------|
| Simple Query | 10ms | 25ms | 2.5x |
| Bulk Insert (1000) | 100ms | 500ms | 5x |
| Complex Join | 15ms | 40ms | 2.7x |

## Migration t? EF Core

```csharp
// EF Core
using (var transaction = dbContext.Database.BeginTransaction())
{
    dbContext.Todos.Add(todo1);
    dbContext.Todos.Add(todo2);
    dbContext.SaveChanges();
    transaction.Commit();
}

// Dapper + UnitOfWork
using var uow = new UnitOfWork(factory);
var repo = new TodoRepository(uow);
uow.BeginTransaction();
await repo.CreateAsync(todo1);
await repo.CreateAsync(todo2);
uow.Commit();
```

## Cheat Sheet

```csharp
// Simple CRUD (No transaction)
var repo = new TodoRepository(factory);
await repo.GetByIdAsync(1);

// Multi-step transaction
using var uow = new UnitOfWork(factory);
var repo = new TodoRepository(uow);
uow.BeginTransaction();
/* operations */
uow.Commit();

// Custom isolation
uow.BeginTransaction(IsolationLevel.Serializable);

// Stored procedure
var params = new DynamicParameters();
params.Add("@Id", 1);
params.Add("@Count", dbType: DbType.Int32, direction: ParameterDirection.Output);
await conn.QueryAsync<T>("sp_Name", params, commandType: CommandType.StoredProcedure);
var output = params.Get<int>("@Count");
```

---
**Version:** 1.0 | **Last Updated:** 2024
