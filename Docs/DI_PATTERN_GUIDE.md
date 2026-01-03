# ?? Dependency Injection Pattern - Dapper Enterprise

## ? C?p nh?t: Repository và UnitOfWork qu?n l? qua DI

### Trý?c ðây (? Anti-pattern)
```csharp
public class TodoTransactionService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public async Task CreateMultipleTodosAsync(List<string> titles)
    {
        // ? BAD: T?o UnitOfWork và Repository b?ng new
        using var uow = new UnitOfWork(_connectionFactory);
        var repository = new TodoRepository(uow);
        
        // operations...
    }
}
```

**V?n ð?:**
- ? Khó test (không th? mock repository)
- ? Vi ph?m Dependency Inversion Principle
- ? Tight coupling v?i concrete implementation
- ? Không t?n d?ng DI container

### Bây gi? (? Best Practice)
```csharp
public class TodoTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoTransactionService> _logger;

    // ? GOOD: Inject dependencies qua constructor
    public TodoTransactionService(
        IUnitOfWork unitOfWork,
        ITodoRepository repository,
        ILogger<TodoTransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> CreateMultipleTodosAsync(
        IEnumerable<string> titles, CancellationToken ct = default)
    {
        var newIds = new List<int>();

        try
        {
            _unitOfWork.BeginTransaction();

            foreach (var title in titles)
            {
                var newId = await _repository.CreateAsync(title, ct);
                newIds.Add(newId);
            }

            _unitOfWork.Commit();
            return newIds;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
```

**L?i ích:**
- ? D? test v?i mock
- ? Tuân th? SOLID principles
- ? Loose coupling
- ? DI container qu?n l? lifecycle

## ??? DI Configuration trong Program.cs

```csharp
// Dapper Infrastructure - Enterprise Pattern
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// TodoRepository ðý?c inject v?i IUnitOfWork ð? support transaction
builder.Services.AddScoped<ITodoRepository>(sp => 
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    return new TodoRepository(unitOfWork);
});

// Service layer - inject c? IUnitOfWork và ITodoRepository
builder.Services.AddScoped<ITodoTransactionService, TodoTransactionService>();
```

### Gi?i thích t?ng bý?c:

#### 1. IDbConnectionFactory - Singleton
```csharp
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
```
- **Lifetime:** Singleton
- **L? do:** Stateless, ch? t?o connection
- **Thread-safe:** Yes

#### 2. IUnitOfWork - Scoped
```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```
- **Lifetime:** Scoped (per request)
- **L? do:** Qu?n l? transaction trong request
- **State:** Có (connection, transaction)

#### 3. ITodoRepository - Scoped v?i Factory
```csharp
builder.Services.AddScoped<ITodoRepository>(sp => 
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    return new TodoRepository(unitOfWork);
});
```
- **Lifetime:** Scoped
- **Factory pattern:** Inject UnitOfWork vào constructor
- **L? do:** Repository c?n share connection/transaction v?i UnitOfWork

#### 4. ITodoTransactionService - Scoped
```csharp
builder.Services.AddScoped<ITodoTransactionService, TodoTransactionService>();
```
- **Lifetime:** Scoped
- **Dependencies:** IUnitOfWork, ITodoRepository, ILogger
- **Auto-resolved:** DI container t? ð?ng inject

## ?? T?i sao Repository nh?n UnitOfWork?

### Option 1: Repository standalone (v?i IDbConnectionFactory)
```csharp
public TodoRepository(IDbConnectionFactory factory)
{
    _connectionFactory = factory;
}

// M?i operation t? m?/ðóng connection
public async Task<TodoItemDto?> GetByIdAsync(int id)
{
    using var conn = _connectionFactory.CreateConnection();
    // query...
}
```
**Use case:** Simple queries không c?n transaction

### Option 2: Repository v?i UnitOfWork (? Ðý?c ch?n)
```csharp
public TodoRepository(IUnitOfWork unitOfWork)
{
    _unitOfWork = unitOfWork;
}

// Dùng connection và transaction t? UnitOfWork
public async Task<TodoItemDto?> GetByIdAsync(int id)
{
    var conn = _unitOfWork.Connection;
    var transaction = _unitOfWork.Transaction;
    // query v?i transaction...
}
```
**Use case:** Multi-step operations c?n transaction

**Quy?t ð?nh:** Ch?n Option 2 v?:
- ? Support transaction ða tác v?
- ? Share connection gi?a nhi?u operations
- ? ACID guaranteed
- ? Phù h?p v?i enterprise requirements

## ?? Dependency Graph

```
Request
  ?
  ??? TodoTransactionController
  ?     ?
  ?     ??? ITodoTransactionService (Scoped)
  ?           ?
  ?           ??? IUnitOfWork (Scoped)
  ?           ?     ?
  ?           ?     ??? IDbConnectionFactory (Singleton)
  ?           ?
  ?           ??? ITodoRepository (Scoped)
  ?           ?     ?
  ?           ?     ??? IUnitOfWork (Same instance ?)
  ?           ?
  ?           ??? ILogger
  ?
  ??? End of Request
        ?
        ??? Dispose all Scoped instances
              ?
              ??? UnitOfWork.Dispose()
              ?     ??? Rollback transaction n?u chýa commit
              ?
              ??? Repository (no disposal needed)
```

## ?? Testing v?i DI

### Unit Test Service (v?i Mock)
```csharp
[Fact]
public async Task CreateMultipleTodos_ShouldCommitTransaction()
{
    // Arrange
    var mockUow = new Mock<IUnitOfWork>();
    var mockRepo = new Mock<ITodoRepository>();
    var mockLogger = new Mock<ILogger<TodoTransactionService>>();

    mockRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), default))
        .ReturnsAsync(1);

    var service = new TodoTransactionService(
        mockUow.Object, 
        mockRepo.Object, 
        mockLogger.Object);

    // Act
    var result = await service.CreateMultipleTodosAsync(
        new[] { "Todo 1", "Todo 2" }
    );

    // Assert
    Assert.Equal(2, result.Count);
    mockUow.Verify(u => u.BeginTransaction(), Times.Once);
    mockUow.Verify(u => u.Commit(), Times.Once);
    mockRepo.Verify(r => r.CreateAsync(It.IsAny<string>(), default), Times.Exactly(2));
}
```

### Integration Test (v?i Real DB)
```csharp
[Fact]
public async Task CreateMultipleTodos_RealDatabase()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<ITodoRepository>(sp => 
        new TodoRepository(sp.GetRequiredService<IUnitOfWork>()));
    services.AddScoped<ITodoTransactionService, TodoTransactionService>();
    services.AddLogging();

    var provider = services.BuildServiceProvider();
    
    using var scope = provider.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<ITodoTransactionService>();

    // Act
    var result = await service.CreateMultipleTodosAsync(
        new[] { "Test 1", "Test 2" }
    );

    // Assert
    Assert.Equal(2, result.Count);
}
```

## ?? Request Lifecycle

```
1. HTTP Request arrives
   ?
2. DI Container creates Scoped instances:
   - UnitOfWork (new instance)
   - TodoRepository (inject UnitOfWork)
   - TodoTransactionService (inject UnitOfWork + Repository)
   ?
3. Controller calls Service
   ?
4. Service uses UnitOfWork and Repository
   ?
5. Transaction commits/rollbacks
   ?
6. Request ends
   ?
7. DI Container disposes Scoped instances:
   - UnitOfWork.Dispose()
     ? Auto rollback n?u chýa commit
     ? Close connection
   ?
8. Response sent
```

## ?? Common Pitfalls

### ? Pitfall 1: Mix Singleton và Scoped
```csharp
// BAD: Singleton service inject Scoped dependency
builder.Services.AddSingleton<ISomeService, SomeService>();
// SomeService inject IUnitOfWork ? Error!
```

**Fix:**
```csharp
// Service ph?i Scoped n?u inject Scoped dependencies
builder.Services.AddScoped<ISomeService, SomeService>();
```

### ? Pitfall 2: Quên Dispose Scoped Service
```csharp
// BAD: Không dùng using v?i manual service creation
var service = serviceProvider.GetRequiredService<IUnitOfWork>();
service.BeginTransaction();
// ... forgot to dispose
```

**Fix:**
```csharp
// GOOD: S? d?ng scope
using var scope = serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
service.BeginTransaction();
// Auto dispose khi scope k?t thúc
```

### ? Pitfall 3: Multiple UnitOfWork instances
```csharp
// BAD: T?o 2 UnitOfWork khác nhau
public SomeService(IUnitOfWork uow1, IServiceProvider sp)
{
    var uow2 = sp.GetRequiredService<IUnitOfWork>();
    // uow1 và uow2 là cùng instance (Scoped)
}
```

**Note:** Trong cùng scope, DI container tr? v? cùng instance c?a Scoped service.

## ?? Best Practices Summary

? **DO:**
- Inject dependencies qua constructor
- Dùng interface, không concrete class
- Scoped lifetime cho services có state
- Factory pattern khi c?n custom initialization
- Test v?i mocks

? **DON'T:**
- `new` repository trong service
- Mix Singleton v?i Scoped dependencies
- Store scoped services trong static fields
- Inject IServiceProvider tr?c ti?p (except factories)

## ?? Key Takeaways

1. **Repository và UnitOfWork qu?n l? b?i DI** - Không `new` trong code
2. **Cùng Scoped lifetime** - Share instance trong request
3. **Constructor injection** - Dependencies r? ràng
4. **Testable** - D? mock v?i interfaces
5. **SOLID compliant** - Dependency Inversion Principle

---

**Version:** 2.0 - DI Managed Pattern  
**Updated:** 2024
