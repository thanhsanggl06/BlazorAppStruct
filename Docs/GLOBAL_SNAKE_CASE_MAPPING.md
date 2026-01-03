# ?? Dapper Global Snake_Case Mapping - Setup Guide

## ? Ð? Setup

### 1. Auto-Mapper ð? có trong `Data/Dapper/Infrastructure/DapperSnakeCaseMapper.cs`

Mapper t? ð?ng convert:
- `full_name` ? `FullName`
- `is_done` ? `IsDone`
- `created_at` ? `CreatedAt`
- `user_id` ? `UserId`

### 2. Ð? ðãng k? GLOBAL mapper trong `Server/Program.cs`

```csharp
using Data.Dapper.Infrastructure;

// ? GLOBAL mapping - Áp d?ng cho T?T C? DTOs t? ð?ng
DapperMapperExtensions.RegisterGlobalSnakeCaseMapper();

// ? KHÔNG C?N ðãng k? t?ng DTO n?a:
// DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();  // ? Th?a!
// DapperMapperExtensions.RegisterSnakeCaseMapper<UserDto>();      // ? Th?a!
```

**L?i ích c?a Global Mapper:**
- ? **One-time setup** - Ch? g?i m?t l?n
- ? **T? ð?ng áp d?ng** cho m?i DTO (hi?n t?i và týõng lai)
- ? **Không c?n maintenance** khi thêm DTOs m?i
- ? **Convention over configuration**

### 3. Cách ho?t ð?ng

```csharp
// Trong DapperSnakeCaseMapper.cs
public static void RegisterGlobalSnakeCaseMapper()
{
    // Set default type map factory cho T?T C? types
    SqlMapper.TypeMapProvider = type =>
    {
        return new DapperSnakeCaseMapper(type);
    };
}
```

**Khi Dapper query b?t k? type nào:**
1. Check xem type ð? có mapper chýa
2. N?u chýa ? T? ð?ng t?o `DapperSnakeCaseMapper` cho type ðó
3. Map columns theo convention: `snake_case` ? `PascalCase`

## ?? Cách s? d?ng

### Database Schema (snake_case)
```sql
CREATE TABLE todos (
    id INT PRIMARY KEY,
    full_name NVARCHAR(200),
    is_done BIT,
    created_at DATETIME,
    user_id INT
);
```

### C# DTO (PascalCase) - KHÔNG C?N [Column] attribute
```csharp
public record TodoItemDto
{
    // T? ð?ng map t?: id
    public int Id { get; init; }
    
    // T? ð?ng map t?: full_name
    public string FullName { get; init; } = string.Empty;
    
    // T? ð?ng map t?: is_done
    public bool IsDone { get; init; }
    
    // T? ð?ng map t?: created_at
    public DateTime? CreatedAt { get; init; }
    
    // T? ð?ng map t?: user_id
    public int UserId { get; init; }
}
```

### Repository Query - Clean & Simple
```csharp
public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
{
    // ? Query ðõn gi?n - KHÔNG c?n alias
    const string sql = "SELECT * FROM todos WHERE id = @Id";
    
    // Dapper t? ð?ng map:
    // full_name ? FullName
    // is_done ? IsDone
    // created_at ? CreatedAt
    
    return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(
        new CommandDefinition(sql, new { Id = id }, _unitOfWork.Transaction, cancellationToken: ct)
    );
}

public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default)
{
    // ? SELECT * works perfectly
    const string sql = "SELECT * FROM todos ORDER BY created_at DESC";
    
    var results = await _unitOfWork.Connection.QueryAsync<TodoItemDto>(
        new CommandDefinition(sql, transaction: _unitOfWork.Transaction, cancellationToken: ct)
    );
    
    return results.ToList();
}
```

## ?? Mapping Rules

| Database (snake_case) | C# Property (PascalCase) | Auto-mapped? |
|----------------------|-------------------------|--------------|
| `id` | `Id` | ? Yes |
| `full_name` | `FullName` | ? Yes |
| `is_done` | `IsDone` | ? Yes |
| `created_at` | `CreatedAt` | ? Yes |
| `user_id` | `UserId` | ? Yes |
| `first_name_middle_name` | `FirstNameMiddleName` | ? Yes |

**Algorithm:**
```
1. Split by underscore: "full_name" ? ["full", "name"]
2. Capitalize each part: ["Full", "Name"]
3. Join: "FullName"
```

## ?? Best Practices

### ? DO:

```csharp
// 1. Register mapper at application startup
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();

// 2. Use simple queries
const string sql = "SELECT * FROM todos WHERE id = @Id";

// 3. Consistent naming convention
// Database: snake_case
// C#: PascalCase
```

### ? DON'T:

```csharp
// 1. Don't mix naming conventions
public class BadDto
{
    public int id { get; set; }  // ? lowercase
    public string FullName { get; set; }  // ? PascalCase
}

// 2. Don't use column alias if not needed
const string sql = "SELECT id AS Id, full_name AS FullName FROM todos";  // ? Verbose

// 3. Don't register mapper multiple times
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();  // ? Duplicate
```

## ?? Advanced Usage

### Multiple DTOs
```csharp
// Register all at once
DapperMapperExtensions.RegisterSnakeCaseMappers(
    typeof(TodoItemDto),
    typeof(UserDto),
    typeof(OrderDto),
    typeof(ProductDto)
);
```

### Custom Mapping (if needed)
```csharp
// N?u c?n override cho m?t property c? th?
const string sql = @"
    SELECT 
        *,
        CONCAT(first_name, ' ', last_name) AS FullName  -- Custom logic
    FROM users";
```

### Complex Queries
```csharp
// JOIN queries
const string sql = @"
    SELECT 
        t.*,
        u.full_name,
        u.email
    FROM todos t
    INNER JOIN users u ON t.user_id = u.id";

// Multi-mapping
var todos = await conn.QueryAsync<TodoItemDto, UserDto, TodoItemDto>(
    sql,
    (todo, user) => 
    {
        todo.User = user;
        return todo;
    },
    splitOn: "full_name"  // Start of user columns
);
```

## ?? How It Works

### Mapper Implementation
```csharp
private static string ConvertSnakeCaseToPascalCase(string snakeCase)
{
    if (string.IsNullOrWhiteSpace(snakeCase))
        return snakeCase;

    var parts = snakeCase.Split('_');
    var result = string.Join("", parts.Select(part => 
        char.ToUpper(part[0]) + part.Substring(1).ToLower()
    ));

    return result;
}
```

### Registration
```csharp
public static void RegisterSnakeCaseMapper<T>()
{
    SqlMapper.SetTypeMap(typeof(T), new DapperSnakeCaseMapper(typeof(T)));
}
```

## ?? Testing

### Unit Test Example
```csharp
[Fact]
public void SnakeCaseMapper_ShouldMapCorrectly()
{
    // Arrange
    DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();
    
    using var conn = new SqlConnection(connectionString);
    
    // Act
    var result = conn.QueryFirstOrDefault<TodoItemDto>(
        "SELECT 1 AS id, 'Test' AS full_name, 1 AS is_done, GETDATE() AS created_at"
    );
    
    // Assert
    Assert.Equal(1, result.Id);
    Assert.Equal("Test", result.FullName);
    Assert.True(result.IsDone);
    Assert.NotNull(result.CreatedAt);
}
```

## ?? Troubleshooting

### Problem: Property not mapped
```
Error: Property 'FullName' not found
```

**Solution:**
1. Check database column name: `full_name` (snake_case)
2. Check C# property name: `FullName` (PascalCase)
3. Verify mapper registered: `RegisterSnakeCaseMapper<TodoItemDto>()`

### Problem: Wrong casing
```
Database: fullName (camelCase)
C#: FullName (PascalCase)
Result: Not mapped
```

**Solution:**
- Database MUST use snake_case: `full_name`
- Or use SQL alias: `SELECT fullName AS FullName`

### Problem: Mapper not working
```
Still getting null properties
```

**Solution:**
1. Register mapper BEFORE first query:
```csharp
// In Program.cs, before app.Run()
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();
```

2. Clear and rebuild:
```sh
dotnet clean
dotnet build
```

## ?? Comparison: Per-Type vs Global Mapping

### ? Per-Type Registration (Old Way - Verbose)

```csharp
// Program.cs - Ph?i ðãng k? T?NG DTO
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();
DapperMapperExtensions.RegisterSnakeCaseMapper<UserDto>();
DapperMapperExtensions.RegisterSnakeCaseMapper<OrderDto>();
DapperMapperExtensions.RegisterSnakeCaseMapper<ProductDto>();
// ... thêm 100 DTOs n?a? ??
```

**Nhý?c ði?m:**
- ? Ph?i ðãng k? t?ng DTO
- ? D? quên khi thêm DTO m?i
- ? Code dài, khó maintain
- ? Runtime error n?u quên ðãng k?

### ? Global Registration (New Way - Recommended)

```csharp
// Program.cs - Ch? M?T d?ng
DapperMapperExtensions.RegisterGlobalSnakeCaseMapper();

// Done! ? T?t c? DTOs hi?n t?i và týõng lai ð?u ðý?c map t? ð?ng
```

**Ýu ði?m:**
- ? One-time setup
- ? T? ð?ng cho m?i DTO
- ? Convention-based
- ? Không lo quên ðãng k?

---

## ?? When to Use Which?

### Use Global Mapper when:
- ? **Consistent naming**: T?t c? tables dùng snake_case
- ? **Large project**: Nhi?u DTOs (>10)
- ? **Convention-based team**: Team theo convention ch?t ch?
- ? **Future-proof**: Mu?n t? ð?ng cho DTOs m?i

### Use Per-Type Mapper when:
- ?? **Mixed conventions**: M?t s? tables snake_case, m?t s? PascalCase
- ?? **Small project**: Ít DTOs (<5)
- ?? **Selective mapping**: Ch? m?t s? DTOs c?n map
- ?? **Explicit control**: Mu?n control t?ng DTO

---

## ? Summary

**Global snake_case mapping is perfect when:**
- ? ALL database tables use snake_case
- ? ALL C# DTOs use PascalCase
- ? Consistent naming convention trong team
- ? Mu?n clean queries (no alias)

**Setup:**
```csharp
// 1. Program.cs - ONE LINE ONLY
DapperMapperExtensions.RegisterGlobalSnakeCaseMapper();

// 2. Repository - Query clean
const string sql = "SELECT * FROM todos";

// 3. DTOs work automatically - NO registration needed
var todos = await connection.QueryAsync<TodoItemDto>(sql);
var users = await connection.QueryAsync<UserDto>(sql);
var orders = await connection.QueryAsync<OrderDto>(sql);
// All mapped automatically! ?
```

---

**Version:** 2.0 - Global Mapper Edition  
**Implementation:** `Data/Dapper/Infrastructure/DapperSnakeCaseMapper.cs`  
**Registered in:** `Server/Program.cs` (ONE line)  
**Status:** ? Truly global - No per-type registration needed
