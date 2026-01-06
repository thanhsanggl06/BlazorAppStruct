# Fixed Length File Processing Guide

## T?ng quan

H? th?ng x? l? file fixed length v?i attribute-based mapping, h? tr? ð?c/ghi file theo ð?nh d?ng c? ð?nh ð? dài c?t.

## C?u trúc

```
Shared/FixedLength/
??? Attributes/
?   ??? FixedLengthColumnAttribute.cs    # Attribute ð?nh ngh?a c?t
??? Converters/
?   ??? IFixedLengthConverter.cs         # Interface cho custom converter
?   ??? DefaultConverter.cs              # Converter m?c ð?nh
??? Services/
?   ??? FixedLengthFileService.cs        # Service x? l? ð?c/ghi file
??? Models/
    ??? EmployeeRecord.cs                # Model ví d?
    ??? TransactionRecord.cs             # Model v?i custom converter
    ??? VndCurrencyConverter.cs          # Custom converter cho ti?n VND
```

## Cách s? d?ng

### 1. Ð?nh ngh?a Model

```csharp
using Shared.FixedLength.Attributes;

public class EmployeeRecord
{
    // C?t 1: ID nhân viên, 10 k? t?, padding trái b?ng s? 0
    [FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
    public int EmployeeId { get; set; }

    // C?t 2: H? tên, 30 k? t?, padding ph?i b?ng kho?ng tr?ng
    [FixedLengthColumn(Order = 2, Length = 30, Padding = PaddingDirection.Right)]
    public string FullName { get; set; } = string.Empty;

    // C?t 3: Ngày sinh, 8 k? t?, format yyyyMMdd
    [FixedLengthColumn(Order = 3, Length = 8, Format = "yyyyMMdd")]
    public DateTime BirthDate { get; set; }

    // C?t 4: Lýõng, 10 k? t?, format 0000000.00 (7 ch? s? + 2 th?p phân)
    [FixedLengthColumn(Order = 4, Length = 10, Format = "0000000.00")]
    public decimal Salary { get; set; }

    // C?t 5: Ðang làm vi?c, 1 k? t?, Y/N
    [FixedLengthColumn(Order = 5, Length = 1, Format = "Y")]
    public bool IsActive { get; set; }
}
```

### 2. Ghi File

```csharp
var service = new FixedLengthFileService();

var employees = new List<EmployeeRecord>
{
    new() { EmployeeId = 1, FullName = "Nguy?n Vãn A", BirthDate = new DateTime(1990, 5, 15), Salary = 15000000m, IsActive = true },
    new() { EmployeeId = 2, FullName = "Tr?n Th? B", BirthDate = new DateTime(1985, 8, 20), Salary = 20000000m, IsActive = true }
};

// Ghi ra file
await service.WriteFileAsync("employees.txt", employees);

// Ho?c ghi ra stream
using var stream = new MemoryStream();
await service.WriteStreamAsync(stream, employees);
```

**K?t qu? file:**
```
0000000001Nguy?n Vãn A                  19900515150000000YIT      
0000000002Tr?n Th? B                    19850820200000000YHR      
```

### 3. Ð?c File

```csharp
var service = new FixedLengthFileService();

// Ð?c t? file
var employees = await service.ReadFileAsync<EmployeeRecord>("employees.txt");

// Ho?c ð?c t? stream
using var stream = File.OpenRead("employees.txt");
var employees = await service.ReadStreamAsync<EmployeeRecord>(stream);

foreach (var emp in employees)
{
    Console.WriteLine($"{emp.EmployeeId}: {emp.FullName} - {emp.Salary:C}");
}
```

## Attribute Properties

### FixedLengthColumnAttribute

| Property | Type | Mô t? | M?c ð?nh |
|----------|------|-------|----------|
| `Order` | int | Th? t? c?t (b?t ð?u t? 1) | B?t bu?c |
| `Length` | int | Ð? dài c? ð?nh (s? k? t?) | B?t bu?c |
| `PadChar` | char | K? t? padding | `' '` (space) |
| `Padding` | PaddingDirection | Hý?ng padding (Left/Right) | `Right` |
| `Format` | string? | Format string cho DateTime, s? | `null` |
| `TrimOnRead` | bool | Trim kho?ng tr?ng khi ð?c | `true` |
| `DefaultValue` | object? | Giá tr? m?c ð?nh n?u null | `null` |
| `ConverterType` | Type? | Custom converter type | `null` |

### PaddingDirection

- **Right**: Padding bên ph?i (text align left) ? `"ABC   "`
- **Left**: Padding bên trái (text align right) ? `"   ABC"`

## Default Converter

`DefaultConverter` t? ð?ng x? l? các ki?u d? li?u:

| Type | Format m?c ð?nh | Ví d? |
|------|-----------------|-------|
| `DateTime` | `"yyyyMMdd"` | 20240115 |
| `DateOnly` | `"yyyyMMdd"` | 20240115 |
| `TimeOnly` | `"HHmmss"` | 143025 |
| `decimal` | `"F2"` (b? d?u ch?m) | 1234567 (t? 12345.67) |
| `int`, `long` | `"D"` | 123 |
| `bool` | `"Y"` or `"N"` | Y |
| `string` | (không format) | ABC |

## Custom Converter

### T?o Custom Converter

```csharp
using Shared.FixedLength.Converters;

public class VndCurrencyConverter : IFixedLengthConverter
{
    public string? ConvertToString(object? value, int length, string? format)
    {
        if (value == null) return null;
        
        var amount = value switch
        {
            decimal d => (long)d,
            int i => i,
            _ => 0L
        };
        
        return amount.ToString().PadLeft(length, '0');
    }

    public object? ConvertFromString(string value, Type targetType, string? format)
    {
        if (long.TryParse(value, out var amount))
            return (decimal)amount;
        
        return 0m;
    }
}
```

### S? d?ng Custom Converter

```csharp
public class TransactionRecord
{
    [FixedLengthColumn(Order = 1, Length = 12, ConverterType = typeof(VndCurrencyConverter))]
    public decimal Amount { get; set; }
}
```

## Format String Examples

### DateTime / DateOnly

```csharp
[FixedLengthColumn(Order = 1, Length = 8, Format = "yyyyMMdd")]      // 20240115
[FixedLengthColumn(Order = 2, Length = 10, Format = "yyyy-MM-dd")]    // 2024-01-15
[FixedLengthColumn(Order = 3, Length = 14, Format = "yyyyMMddHHmmss")] // 20240115143025
```

### TimeOnly

```csharp
[FixedLengthColumn(Order = 1, Length = 6, Format = "HHmmss")]   // 143025
[FixedLengthColumn(Order = 2, Length = 8, Format = "HH:mm:ss")] // 14:30:25
```

### Decimal / Double

```csharp
[FixedLengthColumn(Order = 1, Length = 10, Format = "0000000.00")]  // 0012345.67
[FixedLengthColumn(Order = 2, Length = 8, Format = "F2")]           // 12345.67 -> 1234567 (b? d?u ch?m)
```

### Integer

```csharp
[FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
public int Id { get; set; }  // 0000000123
```

### Boolean

```csharp
[FixedLengthColumn(Order = 1, Length = 1, Format = "Y")]   // Y ho?c N
[FixedLengthColumn(Order = 2, Length = 1, Format = "S")]   // S ho?c N (Success)
[FixedLengthColumn(Order = 3, Length = 1, Format = "1")]   // 1 ho?c 0
```

## Best Practices

### 1. Luôn xác ð?nh Order r? ràng

```csharp
? GOOD:
[FixedLengthColumn(Order = 1, Length = 10)]
[FixedLengthColumn(Order = 2, Length = 20)]
[FixedLengthColumn(Order = 3, Length = 8)]

? BAD:
[FixedLengthColumn(Length = 10)]  // Thi?u Order
```

### 2. S? d?ng Padding phù h?p

```csharp
? GOOD:
// S?: Padding trái v?i '0'
[FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
public int Id { get; set; }

// Text: Padding ph?i v?i ' '
[FixedLengthColumn(Order = 2, Length = 30, Padding = PaddingDirection.Right)]
public string Name { get; set; }
```

### 3. X? l? Nullable types

```csharp
// Nên cung c?p DefaultValue cho nullable
[FixedLengthColumn(Order = 1, Length = 8, Format = "yyyyMMdd", DefaultValue = "19000101")]
public DateTime? BirthDate { get; set; }
```

### 4. Encoding

```csharp
// M?c ð?nh UTF8
await service.WriteFileAsync("file.txt", records);

// Tùy ch?nh encoding
await service.WriteFileAsync("file.txt", records, Encoding.GetEncoding("windows-1252"));
```

## Ví d? th?c t?

### File Bank Transaction

```csharp
public class BankTransactionRecord
{
    [FixedLengthColumn(Order = 1, Length = 15, Padding = PaddingDirection.Left, PadChar = '0')]
    public long TransactionId { get; set; }

    [FixedLengthColumn(Order = 2, Length = 8, Format = "yyyyMMdd")]
    public DateOnly TransactionDate { get; set; }

    [FixedLengthColumn(Order = 3, Length = 6, Format = "HHmmss")]
    public TimeOnly TransactionTime { get; set; }

    [FixedLengthColumn(Order = 4, Length = 20)]
    public string AccountNumber { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 5, Length = 15, Padding = PaddingDirection.Left, PadChar = '0')]
    public decimal Amount { get; set; }

    [FixedLengthColumn(Order = 6, Length = 3)]
    public string CurrencyCode { get; set; } = "VND";

    [FixedLengthColumn(Order = 7, Length = 1, Format = "S")]
    public bool IsSuccessful { get; set; }

    [FixedLengthColumn(Order = 8, Length = 50)]
    public string Description { get; set; } = string.Empty;
}
```

**Output:**
```
00000000000123420240115143025ACC123456789012345000000000123456VNDSTransfer to John Doe                          
00000000000123520240115150030ACC987654321098765000000000067890VNDSPayment for invoice #12345                    
```

## Error Handling

```csharp
try
{
    var records = await service.ReadFileAsync<EmployeeRecord>("file.txt");
}
catch (FileNotFoundException)
{
    // File không t?n t?i
}
catch (InvalidOperationException ex)
{
    // L?i converter ho?c format không ðúng
}
catch (FormatException ex)
{
    // L?i parse d? li?u
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task WriteAndRead_ShouldPreserveData()
{
    // Arrange
    var service = new FixedLengthFileService();
    var original = new EmployeeRecord
    {
        EmployeeId = 123,
        FullName = "Test User",
        BirthDate = new DateTime(1990, 5, 15),
        Salary = 15000000m,
        IsActive = true
    };

    // Act
    using var stream = new MemoryStream();
    await service.WriteStreamAsync(stream, new[] { original });
    stream.Position = 0;
    var results = await service.ReadStreamAsync<EmployeeRecord>(stream);

    // Assert
    var result = results.First();
    Assert.Equal(original.EmployeeId, result.EmployeeId);
    Assert.Equal(original.FullName, result.FullName);
    Assert.Equal(original.Salary, result.Salary);
}
```

## Demo Page

Truy c?p `/fixed-length-demo` ð? xem demo týõng tác:
- Generate sample data
- Download file v?i format fixed length
- Upload và parse file
- View file content trong raw format

## Limitations

- Không h? tr? nested objects
- Không h? tr? collections trong property
- M?i record = 1 d?ng (không multi-line)
- Encoding ph?i consistent gi?a write/read
