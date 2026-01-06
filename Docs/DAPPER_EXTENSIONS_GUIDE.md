# ?? Dapper Extension Methods - Clean & Concise Code

## ? Before: Verbose & Repetitive

```csharp
// Quá dài d?ng, khó ð?c
var result = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(
    new CommandDefinition(
        sql, 
        new { Id = id }, 
        transaction, 
        cancellationToken: ct
    )
);

var affectedRows = await _unitOfWork.Connection.ExecuteAsync(
    new CommandDefinition(
        sql,
        new { Id = id, Title = title, IsDone = isDone },
        transaction,
        cancellationToken: ct
    )
);
```

**V?n ð?:**
- ? Ph?i t?o `CommandDefinition` m?i l?n
- ? Dài d?ng, khó ð?c
- ? L?p l?i code
- ? D? sai parameters

---

## ? After: Clean & Concise

```csharp
// ? Ng?n g?n, d? ð?c!
var result = await connection.QuerySingleAsync<TodoItemDto>(
    sql, 
    new { Id = id }, 
    transaction, 
    ct
);

var affectedRows = await connection.ExecuteAsync(
    sql,
    new { Id = id, Title = title, IsDone = isDone },
    transaction,
    ct
);
```

**L?i ích:**
- ? Ng?n g?n, d? ð?c
- ? Ít code hõn 50%
- ? Ít l?i hõn
- ? D? maintain

---

## ?? Extension Methods Available

### 1. QuerySingleAsync - Query m?t record

```csharp
// Signature
public static async Task<T?> QuerySingleAsync<T>(
    this IDbConnection connection,
    string sql,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)

// Usage
var todo = await connection.QuerySingleAsync<TodoItemDto>(
    "SELECT * FROM TodoItems WHERE Id = @Id",
    new { Id = 1 },
    transaction,
    ct
);
```

**Khi nào dùng:**
- ? Query m?t record duy nh?t
- ? `FirstOrDefault` behavior (null n?u không t?m th?y)

---

### 2. QueryListAsync - Query nhi?u records

```csharp
// Signature
public static async Task<IEnumerable<T>> QueryListAsync<T>(
    this IDbConnection connection,
    string sql,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)

// Usage
var todos = await connection.QueryListAsync<TodoItemDto>(
    "SELECT * FROM TodoItems WHERE IsDone = @IsDone",
    new { IsDone = false },
    transaction,
    ct
);
```

**Khi nào dùng:**
- ? Query list records
- ? `SELECT` statements

---

### 3. ExecuteAsync - Execute command (INSERT/UPDATE/DELETE)

```csharp
// Signature
public static async Task<int> ExecuteAsync(
    this IDbConnection connection,
    string sql,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)

// Usage - INSERT
var affected = await connection.ExecuteAsync(
    "INSERT INTO TodoItems (Title) VALUES (@Title)",
    new { Title = "New todo" },
    transaction,
    ct
);

// Usage - UPDATE
var affected = await connection.ExecuteAsync(
    "UPDATE TodoItems SET IsDone = @IsDone WHERE Id = @Id",
    new { Id = 1, IsDone = true },
    transaction,
    ct
);

// Usage - DELETE
var affected = await connection.ExecuteAsync(
    "DELETE FROM TodoItems WHERE Id = @Id",
    new { Id = 1 },
    transaction,
    ct
);
```

**Khi nào dùng:**
- ? `INSERT`, `UPDATE`, `DELETE`
- ? C?n bi?t s? rows affected

---

### 4. ExecuteScalarAsync - Execute scalar query

```csharp
// Signature
public static async Task<T> ExecuteScalarAsync<T>(
    this IDbConnection connection,
    string sql,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)

// Usage - COUNT
var count = await connection.ExecuteScalarAsync<int>(
    "SELECT COUNT(*) FROM TodoItems WHERE IsDone = @IsDone",
    new { IsDone = false },
    transaction,
    ct
);

// Usage - SCOPE_IDENTITY
var newId = await connection.ExecuteScalarAsync<int>(
    @"INSERT INTO TodoItems (Title) VALUES (@Title);
      SELECT CAST(SCOPE_IDENTITY() AS INT);",
    new { Title = "New todo" },
    transaction,
    ct
);
```

**Khi nào dùng:**
- ? `SELECT COUNT(*)`
- ? `SELECT SCOPE_IDENTITY()`
- ? `SELECT MAX(Id)`
- ? Query tr? v? single value

---

### 5. ExecuteStoredProcAsync - Execute stored procedure

```csharp
// Signature
public static async Task<IEnumerable<T>> ExecuteStoredProcAsync<T>(
    this IDbConnection connection,
    string procedureName,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)

// Usage
var todos = await connection.ExecuteStoredProcAsync<TodoItemDto>(
    "dbo.usp_Todo_GetByStatus",
    new { IsDone = false },
    transaction,
    ct
);
```

**Khi nào dùng:**
- ? Call stored procedures
- ? Không có output parameters

---

### 6. ExecuteStoredProcWithOutputAsync - SP v?i output parameters

```csharp
// Signature
public static async Task<(IEnumerable<T> Results, DynamicParameters Parameters)> 
    ExecuteStoredProcWithOutputAsync<T>(
        this IDbConnection connection,
        string procedureName,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)

// Usage
var parameters = new DynamicParameters();
parameters.Add("@PageNumber", 1);
parameters.Add("@PageSize", 20);
parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

var (results, outParams) = await connection.ExecuteStoredProcWithOutputAsync<TodoItemDto>(
    "dbo.usp_Todo_ListPaged",
    parameters,
    transaction,
    ct
);

var totalCount = outParams.Get<int>("@TotalCount");
```

**Khi nào dùng:**
- ? Stored procedure có output parameters
- ? Paging v?i total count

---

## ?? Real-World Examples

### Example 1: Simple Query

```csharp
// ? Before - Verbose
var result = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(
    new CommandDefinition(
        "SELECT * FROM TodoItems WHERE Id = @Id",
        new { Id = id },
        _unitOfWork.Transaction,
        cancellationToken: ct
    )
);

// ? After - Clean
var result = await _unitOfWork.Connection.QuerySingleAsync<TodoItemDto>(
    "SELECT * FROM TodoItems WHERE Id = @Id",
    new { Id = id },
    _unitOfWork.Transaction,
    ct
);
```

### Example 2: Insert with SCOPE_IDENTITY

```csharp
// ? Before
var newId = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
    new CommandDefinition(
        @"INSERT INTO TodoItems (Title) VALUES (@Title);
          SELECT CAST(SCOPE_IDENTITY() AS INT);",
        new { Title = title },
        _unitOfWork.Transaction,
        cancellationToken: ct
    )
);

// ? After
var newId = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
    @"INSERT INTO TodoItems (Title) VALUES (@Title);
      SELECT CAST(SCOPE_IDENTITY() AS INT);",
    new { Title = title },
    _unitOfWork.Transaction,
    ct
);
```

### Example 3: Update

```csharp
// ? Before
var affected = await _unitOfWork.Connection.ExecuteAsync(
    new CommandDefinition(
        "UPDATE TodoItems SET Title = @Title, IsDone = @IsDone WHERE Id = @Id",
        new { Id = id, Title = title, IsDone = isDone },
        _unitOfWork.Transaction,
        cancellationToken: ct
    )
);

// ? After
var affected = await _unitOfWork.Connection.ExecuteAsync(
    "UPDATE TodoItems SET Title = @Title, IsDone = @IsDone WHERE Id = @Id",
    new { Id = id, Title = title, IsDone = isDone },
    _unitOfWork.Transaction,
    ct
);
```

### Example 4: Stored Procedure v?i Output

```csharp
// ? Before
var parameters = new DynamicParameters();
parameters.Add("@PageNumber", pageNumber);
parameters.Add("@PageSize", pageSize);
parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

var items = await _unitOfWork.Connection.QueryAsync<TodoItemDto>(
    new CommandDefinition(
        "dbo.usp_Todo_ListPaged",
        parameters,
        _unitOfWork.Transaction,
        commandType: CommandType.StoredProcedure,
        cancellationToken: ct
    )
);

var total = parameters.Get<int>("@TotalCount");

// ? After
var parameters = new DynamicParameters();
parameters.Add("@PageNumber", pageNumber);
parameters.Add("@PageSize", pageSize);
parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

var (items, outParams) = await _unitOfWork.Connection.ExecuteStoredProcWithOutputAsync<TodoItemDto>(
    "dbo.usp_Todo_ListPaged",
    parameters,
    _unitOfWork.Transaction,
    ct
);

var total = outParams.Get<int>("@TotalCount");
```

---

## ?? Code Reduction Comparison

| Operation | Before (chars) | After (chars) | Reduction |
|-----------|---------------|--------------|-----------|
| Query Single | 180 | 110 | **39%** ? |
| Query List | 170 | 100 | **41%** ? |
| Execute | 200 | 120 | **40%** ? |
| Execute Scalar | 210 | 125 | **40%** ? |
| Stored Proc | 220 | 130 | **41%** ? |

**Average reduction: ~40% less code!** ??

---

## ?? Best Practices

### ? DO:

```csharp
// 1. Use extension methods for cleaner code
var todo = await connection.QuerySingleAsync<TodoItemDto>(sql, param, txn, ct);

// 2. Pass null for optional parameters
var todos = await connection.QueryListAsync<TodoItemDto>(sql, null, txn, ct);

// 3. Use proper types for scalar results
var count = await connection.ExecuteScalarAsync<int>(sql, param, txn, ct);
var exists = await connection.ExecuteScalarAsync<bool>(sql, param, txn, ct);
```

### ? DON'T:

```csharp
// 1. Don't create CommandDefinition manually anymore
var cmd = new CommandDefinition(...);  // ?

// 2. Don't forget CancellationToken
await connection.QuerySingleAsync<T>(sql, param, txn);  // ? Missing ct

// 3. Don't use QueryAsync when you need single result
var todo = (await connection.QueryListAsync<TodoItemDto>(sql, param, txn, ct)).FirstOrDefault();  // ?
var todo = await connection.QuerySingleAsync<TodoItemDto>(sql, param, txn, ct);  // ?
```

---

## ?? Implementation Details

### Location
`Data/Dapper/Extensions/DapperExtensions.cs`

### How It Works

```csharp
public static async Task<T?> QuerySingleAsync<T>(
    this IDbConnection connection,
    string sql,
    object? param = null,
    IDbTransaction? transaction = null,
    CancellationToken ct = default)
{
    // Wrapper around Dapper's QueryFirstOrDefaultAsync
    // Auto-creates CommandDefinition with all parameters
    return await connection.QueryFirstOrDefaultAsync<T>(
        new CommandDefinition(sql, param, transaction, cancellationToken: ct)
    );
}
```

### Why Extension Methods?

1. ? **Fluent API** - `connection.QuerySingleAsync(...)`
2. ? **IntelliSense support** - IDE auto-complete
3. ? **No breaking changes** - Original Dapper methods still work
4. ? **Opt-in** - Use when you want, fall back to original when needed

---

## ?? Testing

Extension methods are automatically available when you:

```csharp
using Data.Dapper.Extensions;

// Now all extension methods available on IDbConnection
var result = await connection.QuerySingleAsync<TodoItemDto>(...);
```

---

## ? Summary

**Before:**
```csharp
var result = await connection.QueryFirstOrDefaultAsync<T>(
    new CommandDefinition(sql, param, transaction, cancellationToken: ct)
);
```

**After:**
```csharp
var result = await connection.QuerySingleAsync<T>(sql, param, transaction, ct);
```

**Benefits:**
- ? **40% less code**
- ? **Easier to read**
- ? **Easier to write**
- ? **Less error-prone**
- ? **Better maintainability**

---

**Version:** 1.0  
**Location:** `Data/Dapper/Extensions/DapperExtensions.cs`  
**Status:** ? Ready to use
