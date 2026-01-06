# ?? Fixed Length File Download - Quick Guide

## ?? Overview

Extension c?a File Download API ð? support **Fixed Length Files** - Employee, Transaction, và Invoice records.

---

## ?? Quick Start

### 1?? Download Employee Records (Fixed Length)

```csharp
// Client
var request = new FileDownloadRequest 
{ 
    FileIdentifier = "fixed-employee",
    Parameters = new() { { "count", "50" } }  // 50 records
};

var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);

if (result.Success)
{
    // File: employees_20240115_143022.txt
    // Format: Fixed length v?i EmployeeRecord schema
}
```

### 2?? Download Transaction Records

```csharp
var request = new FileDownloadRequest 
{ 
    FileIdentifier = "fixed-transaction",
    Parameters = new() { { "count", "100" } }
};

var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);
// File: transactions_20240115_143022.txt
```

### 3?? Download Invoice Records

```csharp
var request = new FileDownloadRequest 
{ 
    FileIdentifier = "fixed-invoice",
    Parameters = new() { { "count", "25" } }
};

var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);
// File: invoices_20240115_143022.txt
```

### 4?? Download Multiple Fixed Length Files (ZIP)

```csharp
var request = new MultipleFilesDownloadRequest
{
    FileIdentifiers = new() 
    { 
        "fixed-employee",      // Employee records
        "fixed-transaction",   // Transaction records
        "fixed-invoice"        // Invoice records
    },
    ZipFileName = "business-data.zip"
};

var result = await ApiClient.DownloadMultipleFilesAsync("/api/filedownload/multiple", request);
```

---

## ?? Available Fixed Length File Types

| File Type | Identifier | Icon | Description |
|-----------|------------|------|-------------|
| **Employee Records** | `fixed-employee` | ?? | HR employee data (ID, Name, Salary, Department) |
| **Transaction Records** | `fixed-transaction` | ?? | Financial transactions (ID, Date, Amount, Customer) |
| **Invoice Records** | `fixed-invoice` | ?? | Billing/Invoice data (Invoice #, Customer, Amount, Tax) |

---

## ?? Server Implementation

### Controller Method

```csharp
[HttpPost("single")]
public async Task<IActionResult> DownloadSingleFile([FromBody] FileDownloadRequest request)
{
    var (fileBytes, fileName, contentType) = request.FileIdentifier.ToLower() switch
    {
        "fixed-employee" => await GenerateFixedEmployeeFile(request),
        "fixed-transaction" => await GenerateFixedTransactionFile(request),
        "fixed-invoice" => await GenerateFixedInvoiceFile(request),
        _ => GenerateDefaultFile(request.FileIdentifier)
    };

    return File(fileBytes, contentType, fileName);
}
```

### Employee File Generator

```csharp
private async Task<(byte[], string, string)> GenerateFixedEmployeeFile(
    FileDownloadRequest request)
{
    // Parse record count from parameters
    var count = 20;
    if (request.Parameters?.TryGetValue("count", out var countStr) == true)
    {
        int.TryParse(countStr, out count);
        count = Math.Max(1, Math.Min(count, 1000)); // Limit 1-1000
    }

    // Generate sample data
    var employees = SampleDataGenerator.GenerateEmployees(count);
    
    // Write to fixed length format
    using var stream = new MemoryStream();
    await _fixedLengthService.WriteStreamAsync(stream, employees);
    
    var fileName = $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
    return (stream.ToArray(), fileName, "text/plain");
}
```

---

## ?? UI Implementation (Blazor)

### File Type Selector

```razor
<select class="form-select" @bind="selectedFileType">
    <optgroup label="Sample Files">
        <option value="sample-pdf">?? Sample PDF</option>
        <option value="sample-csv">?? Sample CSV</option>
    </optgroup>
    <optgroup label="Fixed Length Files">
        <option value="fixed-employee">?? Employee Records</option>
        <option value="fixed-transaction">?? Transaction Records</option>
        <option value="fixed-invoice">?? Invoice Records</option>
    </optgroup>
</select>
```

### Record Count Input

```razor
@if (IsFixedLengthFile(selectedFileType))
{
    <div class="mb-3">
        <label class="form-label">S? lý?ng records:</label>
        <input type="number" class="form-control" 
               @bind="recordCount" min="1" max="1000" />
        <small class="text-muted">Min: 1, Max: 1000</small>
    </div>
}

@code {
    private bool IsFixedLengthFile(string fileType) 
        => fileType.StartsWith("fixed-");
}
```

### Multiple Files Checkboxes

```razor
<div class="mb-3">
    <label class="form-label"><strong>Fixed Length Files:</strong></label>
    <div class="form-check">
        <input class="form-check-input" type="checkbox" 
               id="check-employee" @bind="includeEmployee">
        <label class="form-check-label" for="check-employee">
            ?? Employee Records
        </label>
    </div>
    <div class="form-check">
        <input class="form-check-input" type="checkbox" 
               id="check-transaction" @bind="includeTransaction">
        <label class="form-check-label" for="check-transaction">
            ?? Transaction Records
        </label>
    </div>
    <div class="form-check">
        <input class="form-check-input" type="checkbox" 
               id="check-invoice" @bind="includeInvoice">
        <label class="form-check-label" for="check-invoice">
            ?? Invoice Records
        </label>
    </div>
</div>
```

---

## ?? File Formats

### Employee Record Format (Fixed Length)

```
Position  Length  Field          Format
1-10      10      EmployeeId     Left-pad with '0'
11-40     30      FullName       Right-pad with space
41-48     8       BirthDate      yyyyMMdd
49-58     10      Salary         0000000.00
59-59     1       IsActive       Y/N
60-74     15      Department     Right-pad with space
```

**Example:**
```
0000001001Nguy?n Vãn A                  199001051500000.00YIT             
0000001002Tr?n Th? B                    198512202000000.00YHR             
```

### Transaction Record Format

```
Position  Length  Field              Format
1-15      15      TransactionId      Left-pad with '0'
16-23     8       TransactionDate    yyyyMMdd
24-29     6       TransactionTime    HHmmss
30-41     12      Amount             VND (custom converter)
42-61     20      CustomerCode       Right-pad
62-111    50      Description        Right-pad
112-112   1       IsSuccessful       S/F
```

### Invoice Record Format

```
Position  Length  Field          Format
1-12      12      InvoiceNumber  Left-pad with '0'
13-20     8       InvoiceDate    yyyyMMdd
21-28     8       DueDate        yyyyMMdd
29-48     20      CustomerCode   Right-pad
49-98     50      CustomerName   Right-pad
99-113    15      SubTotal       000000000000.00
114-128   15      TaxAmount      000000000000.00
129-143   15      TotalAmount    000000000000.00
144-153   10      Status         Right-pad
154-154   1       IsPaid         Y/N
155-184   30      Reference      Right-pad
185-284   100     Notes          Right-pad
```

---

## ?? Configuration

### Record Count Limits

- **Minimum:** 1 record
- **Maximum:** 1000 records
- **Default:** 
  - Employee: 20
  - Transaction: 30
  - Invoice: 15

### File Naming Convention

```
{type}_{timestamp}.txt

Examples:
- employees_20240115_143022.txt
- transactions_20240115_143022.txt
- invoices_20240115_143022.txt
```

---

## ?? Complete Example

### Razor Component

```razor
@page "/export-data"
@inject IApiClient ApiClient
@inject IJSRuntime JS

<h3>Export Business Data</h3>

<div class="mb-3">
    <label>File Type:</label>
    <select @bind="fileType">
        <option value="fixed-employee">Employees</option>
        <option value="fixed-transaction">Transactions</option>
        <option value="fixed-invoice">Invoices</option>
    </select>
</div>

<div class="mb-3">
    <label>Record Count:</label>
    <input type="number" @bind="recordCount" min="1" max="1000" />
</div>

<button @onclick="Export" disabled="@isExporting">
    Export to File
</button>

@code {
    private string fileType = "fixed-employee";
    private int recordCount = 50;
    private bool isExporting;

    private async Task Export()
    {
        isExporting = true;
        
        var request = new FileDownloadRequest
        {
            FileIdentifier = fileType,
            Parameters = new() { { "count", recordCount.ToString() } }
        };

        var result = await ApiClient.DownloadFileAsync(
            "/api/filedownload/single", request);

        if (result.Success && result.FileBytes != null)
        {
            var base64 = Convert.ToBase64String(result.FileBytes);
            await JS.InvokeVoidAsync("downloadFile", 
                result.FileName, base64);
        }

        isExporting = false;
    }
}
```

---

## ?? Testing

### Test Employee Export

```bash
# Request
POST /api/filedownload/single
{
  "fileIdentifier": "fixed-employee",
  "parameters": { "count": "50" }
}

# Response
File: employees_20240115_143022.txt
Content-Type: text/plain
Size: ~3.5 KB (50 records × 74 bytes)
```

### Test Multiple Export (ZIP)

```bash
# Request
POST /api/filedownload/multiple
{
  "fileIdentifiers": [
    "fixed-employee",
    "fixed-transaction",
    "fixed-invoice"
  ],
  "zipFileName": "business-data.zip"
}

# Response
File: business-data.zip
Contains:
  - employees_xxx.txt
  - transactions_xxx.txt
  - invoices_xxx.txt
```

---

## ? Features

? **Dynamic record generation** v?i SampleDataGenerator  
? **Configurable counts** (1-1000)  
? **Fixed length formatting** automatic  
? **Timestamp in filename** cho uniqueness  
? **ZIP support** cho batch export  
? **Type-safe requests** v?i records  
? **Real-world data** (Vietnamese names, realistic amounts)  
? **Production-ready formats**  

---

## ?? Use Cases

1. **HR Data Export**
   - Employee roster
   - Payroll integration
   - Bank transfer files

2. **Financial Reporting**
   - Transaction logs
   - Daily settlement files
   - Bank reconciliation

3. **Billing System**
   - Invoice batch export
   - Tax reporting
   - Customer statements

4. **System Integration**
   - Legacy system data exchange
   - Mainframe file imports
   - EDI file preparation

---

## ?? Related Documentation

- **Main Guide:** [README_FILE_DOWNLOAD.md](../README_FILE_DOWNLOAD.md)
- **Fixed Length Files:** [README_FIXED_LENGTH.md](../README_FIXED_LENGTH.md)
- **API Reference:** [Docs/FILE_DOWNLOAD_QUICK_REF.md](FILE_DOWNLOAD_QUICK_REF.md)

---

**Demo:** `/file-download-demo`

**Status:** ? Production Ready
