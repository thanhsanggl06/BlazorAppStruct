# ?? File Download Guide - ApiClient Extension

## T?ng quan

Extension cho `ApiClient` ð? h? tr? download file t? server s? d?ng **POST method**. H? tr? c? single file và multiple files (ZIP).

---

## ??? Ki?n trúc

```
Shared/Contracts/
  ??? FileDownloadInfo.cs          # Shared models (FileDownloadRequest, MultipleFilesDownloadRequest, FileDownloadInfo)

Client/Services/
  ??? ApiClient.cs                 # Extended v?i DownloadFileAsync, DownloadMultipleFilesAsync

Server/Controllers/
  ??? FileDownloadController.cs    # Demo controller v?i file generation

Client/Pages/
  ??? FileDownloadDemo.razor       # Demo page v?i UI interactive
```

---

## ?? Shared Models

### 1. FileDownloadInfo
Thông tin file - dùng chung gi?a Client và Server

```csharp
public record FileDownloadInfo
{
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public long? FileSize { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
```

### 2. FileDownloadRequest
Request ð? download 1 file

```csharp
public record FileDownloadRequest
{
    public required string FileIdentifier { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}
```

### 3. MultipleFilesDownloadRequest
Request ð? download nhi?u files (ZIP)

```csharp
public record MultipleFilesDownloadRequest
{
    public required List<string> FileIdentifiers { get; init; }
    public string? ZipFileName { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}
```

---

## ?? Client - ApiClient Extension

### Interface Methods

```csharp
public interface IApiClient
{
    // Existing methods...
    
    Task<FileDownloadResult> DownloadFileAsync(
        string url, 
        object? body = null, 
        CancellationToken ct = default);
        
    Task<MultipleFilesDownloadResult> DownloadMultipleFilesAsync(
        string url, 
        object body, 
        CancellationToken ct = default);
}
```

### Download Single File

```csharp
var request = new FileDownloadRequest
{
    FileIdentifier = "sample-pdf",
    Parameters = new Dictionary<string, string>
    {
        { "format", "A4" },
        { "quality", "high" }
    }
};

var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);

if (result.Success && result.FileBytes != null)
{
    // Trigger browser download
    var base64 = Convert.ToBase64String(result.FileBytes);
    await JS.InvokeVoidAsync("downloadFile", result.FileName, base64);
    
    Console.WriteLine($"Downloaded: {result.FileName} ({result.FileBytes.Length} bytes)");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Download Multiple Files (ZIP)

```csharp
var request = new MultipleFilesDownloadRequest
{
    FileIdentifiers = new List<string> { "sample-pdf", "sample-csv", "sample-txt" },
    ZipFileName = "my-files.zip"
};

var result = await ApiClient.DownloadMultipleFilesAsync("/api/filedownload/multiple", request);

if (result.Success && result.ZipBytes != null)
{
    var base64 = Convert.ToBase64String(result.ZipBytes);
    await JS.InvokeVoidAsync("downloadFile", result.ZipFileName, base64);
    
    Console.WriteLine($"Downloaded ZIP: {result.ZipFileName} with {result.FileCount} files");
}
```

---

## ?? Server - Controller Implementation

### Single File Download

```csharp
[HttpPost("single")]
public IActionResult DownloadSingleFile([FromBody] FileDownloadRequest request)
{
    // Generate or retrieve file
    var fileBytes = GenerateFile(request.FileIdentifier);
    var fileName = "sample.pdf";
    var contentType = "application/pdf";
    
    // Set custom header (optional)
    Response.Headers.Append("X-File-Name", fileName);
    
    // Return file with Content-Disposition
    return File(fileBytes, contentType, fileName);
}
```

### Multiple Files Download (ZIP)

```csharp
[HttpPost("multiple")]
public IActionResult DownloadMultipleFiles([FromBody] MultipleFilesDownloadRequest request)
{
    using var memoryStream = new MemoryStream();
    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
    {
        foreach (var fileId in request.FileIdentifiers)
        {
            var (fileBytes, fileName, _) = GenerateFile(fileId);
            
            var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
            using var entryStream = zipEntry.Open();
            entryStream.Write(fileBytes, 0, fileBytes.Length);
        }
    }
    
    var zipBytes = memoryStream.ToArray();
    var zipFileName = request.ZipFileName ?? "download.zip";
    
    // Set custom headers
    Response.Headers.Append("X-File-Name", zipFileName);
    Response.Headers.Append("X-File-Count", request.FileIdentifiers.Count.ToString());
    
    return File(zipBytes, "application/zip", zipFileName);
}
```

---

## ?? Frontend - Blazor Page

### Inject Services

```razor
@inject IApiClient ApiClient
@inject IJSRuntime JS
```

### Download Button

```razor
<button class="btn btn-primary" @onclick="DownloadFile" disabled="@isDownloading">
    @if (isDownloading)
    {
        <span class="spinner-border spinner-border-sm"></span>
        <span>Downloading...</span>
    }
    else
    {
        <span>Download File</span>
    }
</button>
```

### Download Logic

```csharp
private bool isDownloading;

private async Task DownloadFile()
{
    isDownloading = true;
    
    try
    {
        var request = new FileDownloadRequest { FileIdentifier = "sample-pdf" };
        var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);
        
        if (result.Success && result.FileBytes != null)
        {
            await DownloadFileToClient(result.FileBytes, result.FileName ?? "download.bin");
        }
    }
    finally
    {
        isDownloading = false;
    }
}

private async Task DownloadFileToClient(byte[] fileBytes, string fileName)
{
    var base64 = Convert.ToBase64String(fileBytes);
    await JS.InvokeVoidAsync("downloadFile", fileName, base64);
}
```

---

## ?? Header Extraction

Client t? ð?ng extract metadata t? response headers:

### Content-Disposition Header
```
Content-Disposition: attachment; filename="report.pdf"
```

### Custom Headers
```
X-File-Name: report.pdf
X-File-Count: 5
```

### Implementation
```csharp
private static string? GetFileNameFromHeaders(HttpResponseMessage response)
{
    // 1. T? Content-Disposition
    if (response.Content.Headers.ContentDisposition?.FileName is { } fileName)
        return fileName.Trim('"');
    
    // 2. T? custom header
    if (response.Headers.TryGetValues("X-File-Name", out var values))
        return values.FirstOrDefault();
    
    return null;
}
```

---

## ? Features

### ? Single File Download
- ? POST method v?i request body
- ? T? ð?ng extract filename t? headers
- ? H? tr? custom parameters
- ? Error handling chi ti?t
- ? Progress indication

### ??? Multiple Files Download (ZIP)
- ? Download nhi?u files trong 1 request
- ? Auto-generate ZIP file server-side
- ? Custom ZIP filename
- ? File count metadata
- ? Compression optimization

### ?? Client Features
- ? Type-safe requests v?i records
- ? Async/await pattern
- ? CancellationToken support
- ? Browser download trigger (JavaScript interop)
- ? File size formatting helper

---

## ?? Demo Page Features

### Single File Section
- Select file type (PDF, CSV, TXT, JSON)
- Download button v?i loading state
- Success/error messages
- Last download info display

### Multiple Files Section
- Checkbox selection cho multiple files
- Custom ZIP filename input
- File count indicator
- ZIP info display

### Available Files Table
- List t?t c? files available
- File info (name, type, size)
- Quick download action

---

## ?? Usage Examples

### Example 1: Download Report
```csharp
var request = new FileDownloadRequest
{
    FileIdentifier = "monthly-report",
    Parameters = new()
    {
        { "month", "2024-01" },
        { "format", "pdf" }
    }
};

var result = await ApiClient.DownloadFileAsync("/api/reports/download", request);
```

### Example 2: Download Multiple Invoices
```csharp
var request = new MultipleFilesDownloadRequest
{
    FileIdentifiers = new() { "INV-001", "INV-002", "INV-003" },
    ZipFileName = "invoices-january.zip"
};

var result = await ApiClient.DownloadMultipleFilesAsync("/api/invoices/download-batch", request);
```

### Example 3: Download v?i Cancellation
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await ApiClient.DownloadFileAsync("/api/files/large-file", request, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download timeout after 30 seconds");
}
```

---

## ?? Security Considerations

### Server-side
1. **Validate FileIdentifier**: Tránh path traversal attacks
2. **Check permissions**: Verify user có quy?n download file
3. **File size limits**: Gi?i h?n kích thý?c file
4. **Rate limiting**: Ch?ng abuse

### Client-side
1. **Validate file size**: Ki?m tra trý?c khi download
2. **Handle timeouts**: Set reasonable timeout cho large files
3. **Error handling**: Proper error messages cho users

---

## ?? Best Practices

### 1. S? d?ng POST thay v? GET
- ? H? tr? complex request body
- ? Không expose sensitive data trong URL
- ? Không b? gi?i h?n URL length

### 2. Stream large files
```csharp
// Server
return File(stream, contentType, fileName, enableRangeProcessing: true);

// Client
var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
```

### 3. Proper error handling
```csharp
if (!result.Success)
{
    _logger.LogWarning("Download failed: {Error}", result.ErrorMessage);
    await ShowErrorToast(result.ErrorMessage);
    return;
}
```

### 4. Loading states
```razor
<button disabled="@isDownloading">
    @if (isDownloading)
    {
        <span class="spinner-border"></span>
    }
    Download
</button>
```

---

## ?? Testing

### Test Single Download
```
POST /api/filedownload/single
Content-Type: application/json

{
  "fileIdentifier": "sample-pdf"
}
```

### Test Multiple Download
```
POST /api/filedownload/multiple
Content-Type: application/json

{
  "fileIdentifiers": ["sample-pdf", "sample-csv"],
  "zipFileName": "test.zip"
}
```

### Test Available Files
```
GET /api/filedownload/available
```

---

## ?? References

- [ASP.NET Core File Downloads](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/file-downloads)
- [HttpClient Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [ZIP Archive in .NET](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive)

---

## ?? Navigation

Demo page: `/file-download-demo`

Menu: **File Download** (icon: ??)
