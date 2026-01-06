# Fixed Length File Processing - Quick Start

## ?? T?ng quan

H? th?ng x? l? file fixed length format (ð?nh d?ng ð? dài c? ð?nh) v?i attribute-based mapping cho .NET 9.

**Ð?c ði?m:**
- ? D? s? d?ng v?i attributes
- ? H? tr? t?t c? ki?u d? li?u cõ b?n (int, string, DateTime, decimal, bool...)
- ? Custom converter cho logic ð?c bi?t
- ? Auto padding và formatting
- ? Ð?c/ghi file ho?c stream
- ? Enterprise-ready, d? maintain

## ?? Quick Start

### Bý?c 1: Ð?nh ngh?a Model

```csharp
using Shared.FixedLength.Attributes;

public class EmployeeRecord
{
    [FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
    public int EmployeeId { get; set; }

    [FixedLengthColumn(Order = 2, Length = 30)]
    public string FullName { get; set; } = string.Empty;

    [FixedLengthColumn(Order = 3, Length = 8, Format = "yyyyMMdd")]
    public DateTime BirthDate { get; set; }

    [FixedLengthColumn(Order = 4, Length = 10, Format = "0000000.00")]
    public decimal Salary { get; set; }

    [FixedLengthColumn(Order = 5, Length = 1, Format = "Y")]
    public bool IsActive { get; set; }
}
```

### Bý?c 2: Ghi File

```csharp
using Shared.FixedLength.Services;

var service = new FixedLengthFileService();

var employees = new List<EmployeeRecord>
{
    new() 
    { 
        EmployeeId = 1, 
        FullName = "Nguy?n Vãn A", 
        BirthDate = new DateTime(1990, 5, 15), 
        Salary = 15000000m, 
        IsActive = true 
    }
};

await service.WriteFileAsync("employees.txt", employees);
```

### Bý?c 3: Ð?c File

```csharp
var employees = await service.ReadFileAsync<EmployeeRecord>("employees.txt");

foreach (var emp in employees)
{
    Console.WriteLine($"{emp.EmployeeId}: {emp.FullName}");
}
```

## ?? Ví d? Output File

```
0000000001Nguy?n Vãn A                  19900515150000000Y
0000000002Tr?n Th? B                    19850820200000000Y
0000000003Lê Vãn C                      19920310125000000N
```

M?i d?ng có ð? dài c? ð?nh:
- `0000000001` - ID (10 k? t?)
- `Nguy?n Vãn A                  ` - Tên (30 k? t?)
- `19900515` - Ngày sinh (8 k? t?)
- `150000000` - Lýõng (10 k? t?)
- `Y` - Active (1 k? t?)

**T?ng: 59 k? t?/d?ng**

## ?? Demo Page

Vào `/fixed-length-demo` ð? xem demo týõng tác v?i:
- Generate sample data
- Download file fixed length
- Upload và parse file
- View raw file content

## ?? Các ki?u d? li?u h? tr?

| Type | Format m?c ð?nh | Ví d? |
|------|-----------------|-------|
| `int`, `long` | `"D"` | `123` |
| `decimal`, `double` | `"F2"` | `12345.67` ? `1234567` |
| `DateTime` | `"yyyyMMdd"` | `20240115` |
| `DateOnly` | `"yyyyMMdd"` | `20240115` |
| `TimeOnly` | `"HHmmss"` | `143025` |
| `bool` | `"Y"/"N"` | `Y` ho?c `N` |
| `string` | Không format | `ABC` |

## ?? Custom Converter

T?o converter riêng cho logic ð?c bi?t:

```csharp
public class PhoneNumberConverter : IFixedLengthConverter
{
    public string? ConvertToString(object? value, int length, string? format)
    {
        var phone = value?.ToString() ?? "";
        // Remove dashes: 0123-456-789 -> 0123456789
        return phone.Replace("-", "");
    }

    public object? ConvertFromString(string value, Type targetType, string? format)
    {
        // Format: 0123456789 -> 0123-456-789
        if (value.Length == 10)
            return $"{value.Substring(0, 4)}-{value.Substring(4, 3)}-{value.Substring(7, 3)}";
        return value;
    }
}
```

S? d?ng:

```csharp
[FixedLengthColumn(Order = 1, Length = 10, ConverterType = typeof(PhoneNumberConverter))]
public string PhoneNumber { get; set; } = string.Empty;
```

## ?? Tips

### 1. Padding cho s? vs text

```csharp
// S?: Padding trái v?i '0'
[FixedLengthColumn(Order = 1, Length = 10, Padding = PaddingDirection.Left, PadChar = '0')]
public int Id { get; set; }  // ? 0000000123

// Text: Padding ph?i (m?c ð?nh)
[FixedLengthColumn(Order = 2, Length = 20)]
public string Name { get; set; }  // ? "John Doe            "
```

### 2. Format DateTime

```csharp
[FixedLengthColumn(Order = 1, Length = 8, Format = "yyyyMMdd")]      // 20240115
[FixedLengthColumn(Order = 2, Length = 10, Format = "yyyy-MM-dd")]   // 2024-01-15
[FixedLengthColumn(Order = 3, Length = 14, Format = "yyyyMMddHHmmss")] // 20240115143025
```

### 3. Format s? th?p phân

```csharp
// Lýu s? th?p phân không có d?u ch?m
[FixedLengthColumn(Order = 1, Length = 10, Format = "0000000.00")]
public decimal Amount { get; set; }  // 12345.67 ? 0001234567
```

### 4. Boolean format

```csharp
[FixedLengthColumn(Order = 1, Length = 1, Format = "Y")]  // Y/N
[FixedLengthColumn(Order = 2, Length = 1, Format = "S")]  // S/N (Success)
[FixedLengthColumn(Order = 3, Length = 1, Format = "1")]  // 1/0
```

### 5. Encoding

```csharp
// M?c ð?nh UTF-8
await service.WriteFileAsync("file.txt", records);

// Custom encoding
await service.WriteFileAsync("file.txt", records, Encoding.GetEncoding("windows-1252"));
```

## ?? Documentation

Xem chi ti?t t?i: [`Docs/FIXED_LENGTH_FILE_GUIDE.md`](FIXED_LENGTH_FILE_GUIDE.md)

## ??? Architecture

```
FixedLengthFileService (Service)
    ?
Uses Attributes (FixedLengthColumnAttribute)
    ?
Uses Converters (IFixedLengthConverter, DefaultConverter)
    ?
Reflection ð? map properties ? file columns
```

## ?? Use Cases

1. **Banking**: File giao d?ch ngân hàng ð?nh d?ng c? ð?nh
2. **EDI**: Electronic Data Interchange
3. **Mainframe**: Tích h?p v?i h? th?ng mainframe
4. **Legacy Systems**: Import/Export d? li?u t? h? th?ng c?
5. **Batch Processing**: X? l? file batch v?i format c? ð?nh

## ?? Lýu ?

- M?i record = 1 d?ng (không multi-line)
- Không h? tr? nested objects
- Không h? tr? collections trong property
- Encoding ph?i gi?ng nhau khi write/read
- Order ph?i b?t ð?u t? 1 và liên t?c

## ?? Testing

```csharp
[Fact]
public async Task WriteAndRead_ShouldMatch()
{
    var service = new FixedLengthFileService();
    var original = new EmployeeRecord { EmployeeId = 123, FullName = "Test" };
    
    using var stream = new MemoryStream();
    await service.WriteStreamAsync(stream, new[] { original });
    stream.Position = 0;
    var results = await service.ReadStreamAsync<EmployeeRecord>(stream);
    
    Assert.Equal(original.EmployeeId, results[0].EmployeeId);
}
```
