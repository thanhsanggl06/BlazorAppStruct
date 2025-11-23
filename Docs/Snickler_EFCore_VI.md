# Snickler.EFCore

Các phýõng th?c m? r?ng Fluent ð? ánh x? (map) k?t qu? Stored Procedure sang ð?i tý?ng trong Entity Framework Core.

[![NuGet](https://img.shields.io/nuget/v/Snickler.EFCore.svg)](https://www.nuget.org/packages/Snickler.EFCore)

## S? d?ng

### Th?c thi Stored Procedure

Thêm câu l?nh `using Snickler.EFCore` ð? dùng các extension method.

```csharp
var dbContext = GetDbContext();

dbContext.LoadStoredProc("dbo.SomeSproc")
         .WithSqlParam("fooId", 1)
         .ExecuteStoredProc(handler =>
         {
             var fooResults = handler.ReadToList<FooDto>();
             // x? l? d? li?u tr? v?
         });
```

### X? l? nhi?u t?p k?t qu? (Multiple Result Sets)

```csharp
var dbContext = GetDbContext();

dbContext.LoadStoredProc("dbo.SomeSproc")
         .WithSqlParam("fooId", 1)
         .ExecuteStoredProc(handler =>
         {
             var fooResults = handler.ReadToList<FooDto>();
             handler.NextResult();
             var barResults = handler.ReadToList<BarDto>();
             handler.NextResult();
             var bazResults = handler.ReadToList<BazDto>();
         });
```

### Dùng Output Parameter

```csharp
DbParameter outputParam = null;

var dbContext = GetDbContext();

dbContext.LoadStoredProc("dbo.SomeSproc")
         .WithSqlParam("fooId", 1)
         .WithSqlParam("myOutputParam", dbParam =>
         {
             dbParam.Direction = System.Data.ParameterDirection.Output;
             dbParam.DbType = System.Data.DbType.Int32;
             outputParam = dbParam;
         })
         .ExecuteStoredProc(handler =>
         {
             var fooResults = handler.ReadToList<FooDto>();
             handler.NextResult();
             var barResults = handler.ReadToList<BarDto>();
             handler.NextResult();
             var bazResults = handler.ReadToList<BazDto>();
         });

int outputParamValue = (int)outputParam?.Value;
```

### Output Parameter khi không tr? v? Result Set nào

```csharp
DbParameter outputParam = null;

var dbContext = GetDbContext();

await dbContext.LoadStoredProc("dbo.SomeSproc")
    .WithSqlParam("InputParam1", 1)
    .WithSqlParam("myOutputParam", dbParam =>
    {
        dbParam.Direction = System.Data.ParameterDirection.Output;
        dbParam.DbType = System.Data.DbType.Int16;
        outputParam = dbParam;
    })
    .ExecuteStoredNonQueryAsync();

short outputParamValue = (short)outputParam.Value;
```

### Output Parameter + s? d?ng b? ?nh hý?ng (rows affected)

Ð?m b?o Stored Procedure KHÔNG có `SET NOCOUNT ON`.

```csharp
int numberOfRowsAffected = -1;
DbParameter outputParam = null;

var dbContext = GetDbContext();

numberOfRowsAffected = await dbContext.LoadStoredProc("dbo.SomeSproc")
    .WithSqlParam("InputParam1", 1)
    .WithSqlParam("myOutputParam", dbParam =>
    {
        dbParam.Direction = System.Data.ParameterDirection.Output;
        dbParam.DbType = System.Data.DbType.Int16;
        outputParam = dbParam;
    })
    .ExecuteStoredNonQueryAsync();

short outputParamValue = (short)outputParam.Value;
```

### Thay ð?i th?i gian ch? (Timeout) khi ð?i Stored Procedure

```csharp
DbParameter outputParam = null;
var dbContext = GetDbContext();

// ð?i timeout t? 30s m?c ð?nh thành 300s (5 phút)
await dbContext.LoadStoredProc("dbo.SomeSproc", commandTimeout: 300)
    .WithSqlParam("InputParam1", 1)
    .WithSqlParam("myOutputParam", dbParam =>
    {
        dbParam.Direction = System.Data.ParameterDirection.Output;
        dbParam.DbType = System.Data.DbType.Int16;
        outputParam = dbParam;
    })
    .ExecuteStoredNonQueryAsync();

short outputParamValue = (short)outputParam.Value;
```

## Ghi chú thêm

- Extension `LoadStoredProc` t?o `DbCommand` ki?u StoredProcedure và cho phép thêm tham s? qua `WithSqlParam`.
- `ReadToList<T>` ánh x? c?t tr? v? v?i thu?c tính / record c?a `T` theo tên (có th? dùng `[Column(Name="...")]`).
- Có th? dùng `NextResult()` / `NextResultAsync()` ð? chuy?n sang Result Set ti?p theo.
- `ExecuteStoredNonQuery` / `ExecuteStoredNonQueryAsync` dùng cho SP không tr? v? t?p k?t qu? (ch? th?c thi).
- Khi c?n Output Parameter ph?i ð?t `Direction = Output` và `DbType` týõng ?ng.
- Tránh `SET NOCOUNT ON` n?u mu?n nh?n s? d?ng ?nh hý?ng qua `ExecuteStoredNonQuery*`.

## So sánh nhanh

| T?nh hu?ng | Phýõng th?c |
|------------|-------------|
| Tr? v? 1 result set | `ExecuteStoredProc` + `ReadToList<T>` |
| Nhi?u result set | `ExecuteStoredProc` + `handler.NextResult()` |
| Không có result set | `ExecuteStoredNonQuery` |
| Output param + không có result set | `ExecuteStoredNonQuery` v?i tham s? hý?ng Output |
| Tùy ch?nh timeout | Tham s? `commandTimeout` c?a `LoadStoredProc` |

## M?o

- N?u mapping không kh?p, ki?m tra l?i tên c?t tr? v? và tên property (ho?c `[Column]`).
- Ð?m b?o ki?u d? li?u SP tr? v? týõng thích v?i ki?u property (ví d? `datetime` -> `DateTime`).
- Có th? gói logic truy v?n vào service riêng ð? tái s? d?ng.

---
Tài li?u này là b?n d?ch và di?n gi?i l?i hý?ng d?n s? d?ng Snickler.EFCore.
