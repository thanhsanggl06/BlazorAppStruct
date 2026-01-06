# ?? Quick Reference - File Download

## ?? M?c ðích

Extension cho `ApiClient` ð? download file t? server b?ng **POST method**. H? tr?:
- ? Single file download
- ? Multiple files download (ZIP)
- ? Custom parameters
- ? Type-safe requests

---

## ?? Quick Start

### 1?? Client - Download Single File

```csharp
// Inject
@inject IApiClient ApiClient
@inject IJSRuntime JS

// Download
var request = new FileDownloadRequest { FileIdentifier = "sample-pdf" };
var result = await ApiClient.DownloadFileAsync("/api/filedownload/single", request);

if (result.Success)
{
    var base64 = Convert.ToBase64String(result.FileBytes!);
    await JS.InvokeVoidAsync("downloadFile", result.FileName, base64);
}
```

### 2?? Client - Download Multiple Files (ZIP)

```csharp
var request = new MultipleFilesDownloadRequest
{
    FileIdentifiers = new() { "file1", "file2", "file3" },
    ZipFileName = "my-files.zip"
};

var result = await ApiClient.DownloadMultipleFilesAsync("/api/filedownload/multiple", request);

if (result.Success)
{
    var base64 = Convert.ToBase64String(result.ZipBytes!);
    await JS.InvokeVoidAsync("downloadFile", result.ZipFileName, base64);
}
```

### 3?? Server - Single File

```csharp
[HttpPost("download-report")]
public IActionResult DownloadReport([FromBody] FileDownloadRequest request)
{
    var fileBytes = GenerateReport(request.FileIdentifier);
    return File(fileBytes, "application/pdf", "report.pdf");
}
```

### 4?? Server - Multiple Files (ZIP)

```csharp
[HttpPost("download-batch")]
public IActionResult DownloadBatch([FromBody] MultipleFilesDownloadRequest request)
{
    using var ms = new MemoryStream();
    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
    {
        foreach (var fileId in request.FileIdentifiers)
        {
            var entry = archive.CreateEntry($"{fileId}.pdf");
            using var stream = entry.Open();
            var bytes = GetFileBytes(fileId);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
    
    return File(ms.ToArray(), "application/zip", request.ZipFileName ?? "download.zip");
}
```

---

## ?? Shared Models

```csharp
// Request download 1 file
public record FileDownloadRequest
{
    public required string FileIdentifier { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}

// Request download nhi?u files
public record MultipleFilesDownloadRequest
{
    public required List<string> FileIdentifiers { get; init; }
    public string? ZipFileName { get; init; }
}

// File info (metadata)
public record FileDownloadInfo
{
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
    public long? FileSize { get; init; }
}
```

---

## ?? UI Pattern

```razor
<button class="btn btn-primary" @onclick="Download" disabled="@isDownloading">
    @if (isDownloading)
    {
        <span class="spinner-border spinner-border-sm"></span> Downloading...
    }
    else
    {
        <i class="bi bi-download"></i> Download
    }
</button>

@code {
    private bool isDownloading;
    
    private async Task Download()
    {
        isDownloading = true;
        try
        {
            var result = await ApiClient.DownloadFileAsync("/api/files/download", request);
            if (result.Success)
            {
                await TriggerBrowserDownload(result.FileBytes!, result.FileName!);
            }
        }
        finally
        {
            isDownloading = false;
        }
    }
}
```

---

## ? API Methods

### IApiClient Interface

```csharp
// Download 1 file
Task<FileDownloadResult> DownloadFileAsync(
    string url, 
    object? body = null, 
    CancellationToken ct = default);

// Download nhi?u files (ZIP)
Task<MultipleFilesDownloadResult> DownloadMultipleFilesAsync(
    string url, 
    object body, 
    CancellationToken ct = default);
```

### Result Types

```csharp
public record FileDownloadResult
{
    public bool Success { get; init; }
    public byte[]? FileBytes { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string? ErrorMessage { get; init; }
}

public record MultipleFilesDownloadResult
{
    public bool Success { get; init; }
    public byte[]? ZipBytes { get; init; }
    public string? ZipFileName { get; init; }
    public int? FileCount { get; init; }
    public string? ErrorMessage { get; init; }
}
```

---

## ?? Features

? **POST method** - Complex request body, không expose data trong URL  
? **Type-safe** - Records v?i required properties  
? **Auto filename detection** - T? Content-Disposition ho?c custom headers  
? **ZIP support** - Download nhi?u files trong 1 request  
? **Error handling** - Chi ti?t error messages  
? **Cancellation support** - CancellationToken cho long-running downloads  
? **Progress indication** - Loading states cho UX t?t hõn  

---

## ?? File Structure

```
Shared/Contracts/
  ??? FileDownloadInfo.cs       # Shared models

Client/Services/
  ??? ApiClient.cs              # Download methods

Server/Controllers/
  ??? FileDownloadController.cs # Demo controller

Client/Pages/
  ??? FileDownloadDemo.razor    # Demo page
```

---

## ?? Demo

**URL:** `/file-download-demo`

**Menu:** File Download (icon: ??)

**Features:**
- Download PDF, CSV, TXT, JSON
- Download multiple files as ZIP
- View available files
- File info display

---

## ?? Tips

1. **Large files**: S? d?ng `HttpCompletionOption.ResponseHeadersRead` ð? stream
2. **Timeout**: Set reasonable timeout cho large downloads
3. **Security**: Validate FileIdentifier ð? tránh path traversal
4. **Error handling**: Show user-friendly error messages
5. **Loading state**: Always show progress indicator

---

**Full documentation:** [README_FILE_DOWNLOAD.md](README_FILE_DOWNLOAD.md)
