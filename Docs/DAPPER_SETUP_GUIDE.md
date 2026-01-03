# ?? DAPPER ENTERPRISE SETUP - HÝ?NG D?N S? D?NG

## ?? T?ng quan

D? án này ð? ðý?c setup v?i **Dapper Enterprise Pattern** bao g?m:

- ? **Connection Factory** - Qu?n l? database connections
- ? **Unit of Work** - Qu?n l? transactions
- ? **Repository Pattern** - Data access abstraction
- ? **Transaction Service** - Business logic v?i multi-step transactions

## ??? Ki?n trúc

```
Data/Dapper/
??? Interfaces/
?   ??? IDbConnectionFactory.cs    - Factory pattern cho connections
?   ??? IUnitOfWork.cs             - UnitOfWork pattern cho transactions
?   ??? ITodoRepository.cs         - Repository interface
??? Implementations/
    ??? SqlConnectionFactory.cs    - SQL Server connection factory
    ??? UnitOfWork.cs              - Transaction management
    ??? TodoRepository.cs          - Dapper repository implementation

Services/
??? Interfaces/
?   ??? ITodoTransactionService.cs
??? Implements/
    ??? TodoTransactionService.cs  - 5 use cases v?i multi-step transactions

Server/Controllers/
??? TodoTransactionController.cs   - REST API endpoints
```

## ?? Các tính nãng ð? implement

### 1. Create Multiple (Bulk Insert v?i Transaction)
T?o nhi?u todos trong m?t transaction - all or nothing.

**Endpoint:** `POST /api/todotransaction/create-multiple`

**Request:**
```json
["Buy groceries", "Clean house", "Finish report"]
```

**Response:**
```json
{
  "success": true,
  "message": "Created 3 todos successfully",
  "todoIds": [1, 2, 3]
}
```

### 2. Complete and Summarize (Update + Create)
Ðánh d?u nhi?u todos hoàn thành và t?o todo t?ng k?t.

**Endpoint:** `POST /api/todotransaction/complete-and-summarize`

**Request:**
```json
{
  "todoIds": [1, 2, 3],
  "summaryTitle": "Weekly Summary"
}
```

### 3. Archive Pattern (Read-Update-Create-Delete)
Archive các todos: ðánh d?u done ? t?o b?n copy ? xóa b?n g?c.

**Endpoint:** `POST /api/todotransaction/archive`

**Request:**
```json
{
  "todoIds": [1, 2, 3]
}
```

### 4. Bulk Delete v?i Validation
Xóa hàng lo?t v?i validation - ch? xóa n?u t?t c? todos ð? hoàn thành.

**Endpoint:** `DELETE /api/todotransaction/bulk-delete`

**Request:**
```json
{
  "todoIds": [1, 2, 3]
}
```

### 5. Clone and Archive (Complex Multi-step)
Clone todo ? archive b?n g?c ? t?o audit log.

**Endpoint:** `POST /api/todotransaction/clone-and-archive/{id}`

**Request:**
```json
{
  "newTitle": "Updated version: Complete project documentation"
}
```

## ?? Code Examples

### S? d?ng Repository (Standalone Mode)

```csharp
public class MyService
{
    private readonly IDbConnectionFactory _factory;

    public MyService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<TodoItemDto?> GetTodoAsync(int id)
    {
        var repository = new TodoRepository(_factory);
        return await repository.GetByIdAsync(id);
        // Connection t? ð?ng ðóng
    }
}
```

### S? d?ng UnitOfWork (Transaction Mode)

```csharp
public async Task<int> CreateMultipleTodosAsync(List<string> titles)
{
    using var uow = new UnitOfWork(_connectionFactory);
    var repository = new TodoRepository(uow);

    try
    {
        uow.BeginTransaction();

        foreach (var title in titles)
        {
            await repository.CreateAsync(title);
        }

        uow.Commit();
        return titles.Count;
    }
    catch
    {
        uow.Rollback();
        throw;
    }
}
```

### Custom Transaction v?i Isolation Level

```csharp
using var uow = new UnitOfWork(_connectionFactory);
uow.BeginTransaction(IsolationLevel.Serializable);

try
{
    // Critical operations
    await repository.CreateAsync("Important Todo");
    uow.Commit();
}
catch
{
    uow.Rollback();
    throw;
}
```

## ?? Dapper Mapping

### Auto Mapping
```csharp
// Columns: Id, Title, IsDone, CreatedAt
// Properties kh?p tên ? auto map
var todos = await conn.QueryAsync<TodoItemDto>(sql);
```

### Custom Mapping v?i Alias
```csharp
var sql = "SELECT Id, Title AS TodoTitle FROM TodoItems";
var todos = await conn.QueryAsync<TodoItemDto>(sql);
```

### Stored Procedure
```csharp
var parameters = new DynamicParameters();
parameters.Add("@PageNumber", 1);
parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

var items = await conn.QueryAsync<TodoItemDto>(
    "dbo.usp_Todo_ListPaged",
    parameters,
    commandType: CommandType.StoredProcedure
);

var total = parameters.Get<int>("@TotalCount");
```

## ?? Transaction Best Practices

### 1. Always Use Try-Catch v?i UnitOfWork
```csharp
using var uow = new UnitOfWork(_factory);
try
{
    uow.BeginTransaction();
    // operations...
    uow.Commit();
}
catch
{
    uow.Rollback(); // Explicit rollback
    throw;
}
// uow.Dispose() t? ð?ng rollback n?u chýa commit
```

### 2. Validation Before Execute
```csharp
// Phase 1: Validate
foreach (var id in ids)
{
    var item = await repository.GetByIdAsync(id);
    if (item == null)
        throw new InvalidOperationException("Validation failed");
}

// Phase 2: Execute
foreach (var id in ids)
{
    await repository.DeleteAsync(id);
}
```

### 3. Keep Transactions Short
```csharp
// ? BAD: Long-running transaction
uow.BeginTransaction();
await SomeHeavyOperation();  // Don't do this
await repository.CreateAsync();
uow.Commit();

// ? GOOD: Only database operations
await SomeHeavyOperation();  // Do outside transaction
uow.BeginTransaction();
await repository.CreateAsync();
uow.Commit();
```

## ?? Performance Tips

### 1. Bulk Operations
```csharp
// T?t hõn nhi?u foreach
const string sql = "INSERT INTO TodoItems (Title) VALUES (@Title)";
await conn.ExecuteAsync(sql, todoList, transaction);
```

### 2. Query Multiple Result Sets
```csharp
const string sql = @"
    SELECT * FROM TodoItems;
    SELECT COUNT(*) FROM TodoItems;";

using var multi = await conn.QueryMultipleAsync(sql);
var todos = await multi.ReadAsync<TodoItemDto>();
var count = await multi.ReadSingleAsync<int>();
```

### 3. Unbuffered Queries (Large datasets)
```csharp
// Streaming - không load h?t vào memory
var todos = await conn.QueryAsync<TodoItemDto>(sql, buffered: false);
```

## ?? Testing

### Repository Test
```csharp
[Fact]
public async Task CreateAsync_ShouldReturnNewId()
{
    var factory = new SqlConnectionFactory(configuration);
    var repo = new TodoRepository(factory);

    var newId = await repo.CreateAsync("Test Todo");

    Assert.True(newId > 0);
}
```

### Transaction Test
```csharp
[Fact]
public async Task Transaction_ShouldRollbackOnError()
{
    var service = new TodoTransactionService(factory, logger);
    var titles = new[] { "Todo 1", null }; // null gây l?i

    await Assert.ThrowsAsync<ArgumentException>(
        () => service.CreateMultipleTodosAsync(titles)
    );

    // Verify rollback
    var count = await repository.CountAsync();
    Assert.Equal(0, count);
}
```

## ?? Debugging

### Enable SQL Logging
```csharp
_logger.LogDebug("SQL: {Sql}", sql);
_logger.LogDebug("Params: {@Params}", parameters);
```

### Monitor Connection State
```csharp
_logger.LogDebug("Connection: {State}", connection.State);
```

### Track Transactions
```csharp
public void BeginTransaction(IsolationLevel level)
{
    _logger.LogInformation("BEGIN TRAN: {Level}", level);
    _transaction = Connection.BeginTransaction(level);
}
```

## ?? So sánh Pattern

| Feature | Dapper + UoW | EF Core | ADO.NET |
|---------|--------------|---------|---------|
| Performance | ??? | ?? | ??? |
| Complexity | Medium | Low | High |
| Transaction | Manual | Auto | Manual |
| Mapping | Manual | Auto | Manual |
| Type Safety | ? | ? | ? |
| Change Tracking | ? | ? | ? |

## ? Khi nào dùng Dapper + UnitOfWork

? Multi-step transactions c?n ACID  
? Performance critical operations  
? Complex queries v?i raw SQL  
? Microservices v?i simple data model  
? Bulk operations  
? Stored procedures heavy  

## ? Khi nào KHÔNG nên dùng

? Simple CRUD v?i change tracking  
? Complex domain models v?i relationships  
? Rapid development c?n migrations  
? Team không quen SQL  

## ?? Resources

- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)

## ?? Next Steps

1. ? Setup hoàn t?t
2. ?? Test API endpoints v?i Swagger
3. ?? Customize cho business requirements
4. ?? Add more repositories n?u c?n
5. ?? Implement caching layer (optional)

---

**Happy coding!** ??
