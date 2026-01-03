# Column Mapping Strategies - Dapper

## V?n ð?
Database dùng **snake_case** (e.g. `full_name`, `is_done`)  
C# dùng **PascalCase** (e.g. `FullName`, `IsDone`)

## ? Solution 1: SQL Column Alias (Simple)

```csharp
const string sql = @"
    SELECT 
        id AS Id,
        full_name AS FullName,
        is_done AS IsDone,
        created_at AS CreatedAt
    FROM todos";

var todos = await connection.QueryAsync<TodoDto>(sql);
```

**Pros:**
- ? R? ràng, d? hi?u
- ? Không c?n config
- ? Control t?ng query

**Cons:**
- ? Ph?i vi?t alias cho m?i query
- ? Dài d?ng n?u nhi?u columns

**Use when:**
- Ít queries
- C?n control chính xác

---

## ? Solution 2: [Column] Attribute (Recommended)

### Step 1: Annotate DTO

```csharp
using System.ComponentModel.DataAnnotations.Schema;

public record TodoDto
{
    public int Id { get; init; }
    
    [Column("full_name")]
    public string FullName { get; init; } = string.Empty;
    
    [Column("is_done")]
    public bool IsDone { get; init; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; init; }
}
```

### Step 2: Register Mapper (Program.cs)

```csharp
using Data.Dapper.Infrastructure;

// Before app.Build()
ColumnAttributeMapperExtensions.RegisterColumnAttributeMapper<TodoDto>();
```

### Step 3: Query b?nh thý?ng

```csharp
const string sql = "SELECT * FROM todos WHERE id = @Id";

// Dapper t? ð?ng map:
// full_name ? FullName
// is_done ? IsDone
// created_at ? CreatedAt

var todo = await connection.QueryFirstOrDefaultAsync<TodoDto>(sql);
```

**Pros:**
- ? Declarative mapping
- ? Queries clean (no alias needed)
- ? Flexible (per-property control)
- ? Self-documenting

**Cons:**
- ? Ph?i annotate properties
- ? C?n setup mapper

**Use when:**
- Medium to large projects
- Multiple tables v?i snake_case
- **? RECOMMENDED cho enterprise**

---

## ? Solution 3: Convention-based Auto Mapper

### Step 1: Register Auto Mapper (Program.cs)

```csharp
using Data.Dapper.Infrastructure;

// T? ð?ng convert snake_case ? PascalCase
DapperMapperExtensions.RegisterSnakeCaseMapper<TodoDto>();
```

### Step 2: Query b?nh thý?ng

```csharp
const string sql = "SELECT * FROM todos";

// Automatic mapping:
// full_name ? FullName
// is_done ? IsDone
// created_at ? CreatedAt

var todos = await connection.QueryAsync<TodoDto>(sql);
```

**Pros:**
- ? Hoàn toàn t? ð?ng
- ? Không c?n attribute
- ? Convention-based

**Cons:**
- ? Ít control hõn
- ? Khó debug n?u l?i

**Use when:**
- T?t c? tables dùng snake_case
- Convention strong trong team

---

## ?? Comparison Table

| Strategy | Setup Effort | Query Code | Flexibility | Recommended |
|----------|-------------|------------|-------------|-------------|
| SQL Alias | None | Verbose | High | Small projects |
| [Column] Attribute | Medium | Clean | High | **? BEST** |
| Auto Mapper | Low | Clean | Medium | Convention-heavy |

---

## ?? Real Example

### Database Schema
```sql
CREATE TABLE todos (
    id INT PRIMARY KEY,
    full_name NVARCHAR(200),
    is_done BIT,
    created_at DATETIME,
    user_id INT
);
```

### C# DTO with [Column]
```csharp
public record TodoDto
{
    public int Id { get; init; }
    
    [Column("full_name")]
    public string FullName { get; init; } = "";
    
    [Column("is_done")]
    public bool IsDone { get; init; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; init; }
    
    [Column("user_id")]
    public int UserId { get; init; }
}
```

### Repository Query (Clean!)
```csharp
public async Task<TodoDto?> GetByIdAsync(int id)
{
    const string sql = "SELECT * FROM todos WHERE id = @Id";
    
    return await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoDto>(
        new CommandDefinition(sql, new { Id = id }, _unitOfWork.Transaction)
    );
}
```

---

## ?? Tips

### Tip 1: Mix strategies n?u c?n
```csharp
// Most properties dùng [Column]
public record TodoDto
{
    [Column("full_name")]
    public string FullName { get; init; } = "";
    
    // Override trong query n?u c?n
}

const string sql = @"
    SELECT 
        full_name AS FullName,
        UPPER(status) AS Status  -- Custom logic
    FROM todos";
```

### Tip 2: Computed columns
```csharp
public record TodoDto
{
    [Column("full_name")]
    public string FullName { get; init; } = "";
    
    // NotMapped - không có trong DB
    public string DisplayName => $"Todo: {FullName}";
}
```

### Tip 3: Inheritance
```csharp
public abstract record BaseDto
{
    public int Id { get; init; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; init; }
}

public record TodoDto : BaseDto
{
    [Column("full_name")]
    public string FullName { get; init; } = "";
}
```

---

## ? Recommendation

**Use [Column] Attribute approach:**

1. ? Clear và explicit
2. ? Flexible
3. ? Self-documenting
4. ? IDE support (IntelliSense)
5. ? Easy to debug
6. ? Works well with EF Core migration (n?u có)

**Setup once, benefit forever!**

---

**Version:** 1.0  
**Last Updated:** 2024
