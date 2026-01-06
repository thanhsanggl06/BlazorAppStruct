using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using Shared.Contracts;
using Shared.FixedLength.Models;
using Shared.FixedLength.SampleData;
using Shared.FixedLength.Services;

namespace Server.Controllers;

/// <summary>
/// Demo Controller cho File Download - Single và Multiple files
/// Sử dụng POST method để download files
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileDownloadController : ControllerBase
{
    private readonly ILogger<FileDownloadController> _logger;
    private readonly FixedLengthFileService _fixedLengthService;

    public FileDownloadController(ILogger<FileDownloadController> logger)
    {
        _logger = logger;
        _fixedLengthService = new FixedLengthFileService();
    }

    /// <summary>
    /// Download một file đơn
    /// POST /api/filedownload/single
    /// Body: { "fileIdentifier": "sample-pdf", "parameters": { "format": "A4" } }
    /// </summary>
    [HttpPost("single")]
    public async Task<IActionResult> DownloadSingleFile([FromBody] FileDownloadRequest request)
    {
        try
        {
            _logger.LogInformation("Downloading file: {FileId}", request.FileIdentifier);

            // Demo: Tạo file dựa trên fileIdentifier
            var (fileBytes, fileName, contentType) = request.FileIdentifier.ToLower() switch
            {
                "sample-pdf" => GenerateSamplePdf(),
                "sample-csv" => GenerateSampleCsv(),
                "sample-txt" => GenerateSampleText(),
                "sample-json" => GenerateSampleJson(),
                "fixed-employee" => await GenerateFixedEmployeeFile(request),
                "fixed-transaction" => await GenerateFixedTransactionFile(request),
                "fixed-invoice" => await GenerateFixedInvoiceFile(request),
                _ => GenerateDefaultFile(request.FileIdentifier)
            };

            // Set headers
            Response.Headers.Append("X-File-Name", fileName);
            
            // Return file với Content-Disposition để browser download
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileId}", request.FileIdentifier);
            return BadRequest(ApiResponse.Error<object>(ex.Message, "DOWNLOAD_ERROR"));
        }
    }

    /// <summary>
    /// Download nhiều files dưới dạng ZIP
    /// POST /api/filedownload/multiple
    /// Body: { "fileIdentifiers": ["sample-pdf", "sample-csv", "sample-txt"], "zipFileName": "my-files.zip" }
    /// </summary>
    [HttpPost("multiple")]
    public async Task<IActionResult> DownloadMultipleFiles([FromBody] MultipleFilesDownloadRequest request)
    {
        try
        {
            _logger.LogInformation("Downloading {Count} files as ZIP", request.FileIdentifiers.Count);

            // Tạo ZIP file in-memory
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var fileId in request.FileIdentifiers)
                {
                    var (fileBytes, fileName, _) = fileId.ToLower() switch
                    {
                        "sample-pdf" => GenerateSamplePdf(),
                        "sample-csv" => GenerateSampleCsv(),
                        "sample-txt" => GenerateSampleText(),
                        "sample-json" => GenerateSampleJson(),
                        "fixed-employee" => await GenerateFixedEmployeeFile(new FileDownloadRequest { FileIdentifier = fileId }),
                        "fixed-transaction" => await GenerateFixedTransactionFile(new FileDownloadRequest { FileIdentifier = fileId }),
                        "fixed-invoice" => await GenerateFixedInvoiceFile(new FileDownloadRequest { FileIdentifier = fileId }),
                        _ => GenerateDefaultFile(fileId)
                    };

                    // Thêm file vào ZIP
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZIP file");
            return BadRequest(ApiResponse.Error<object>(ex.Message, "ZIP_ERROR"));
        }
    }

    /// <summary>
    /// Lấy danh sách files có sẵn để download
    /// GET /api/filedownload/available
    /// </summary>
    [HttpGet("available")]
    public ActionResult<ApiResponse<List<FileDownloadInfo>>> GetAvailableFiles()
    {
        var files = new List<FileDownloadInfo>
        {
            // Sample files
            new() { FileName = "sample.pdf", ContentType = "application/pdf", FileSize = 1024 * 50 },
            new() { FileName = "sample.csv", ContentType = "text/csv", FileSize = 1024 * 10 },
            new() { FileName = "sample.txt", ContentType = "text/plain", FileSize = 1024 * 5 },
            new() { FileName = "sample.json", ContentType = "application/json", FileSize = 1024 * 8 },
            
            // Fixed length files
            new() { FileName = "employees.txt", ContentType = "text/plain", FileSize = 1024 * 15, 
                Metadata = new() { { "type", "fixed-length" }, { "format", "employee" } } },
            new() { FileName = "transactions.txt", ContentType = "text/plain", FileSize = 1024 * 20,
                Metadata = new() { { "type", "fixed-length" }, { "format", "transaction" } } },
            new() { FileName = "invoices.txt", ContentType = "text/plain", FileSize = 1024 * 25,
                Metadata = new() { { "type", "fixed-length" }, { "format", "invoice" } } },
        };

        return Ok(ApiResponse.Success(files));
    }

    #region Fixed Length File Generators

    /// <summary>
    /// Generate Employee fixed length file
    /// </summary>
    private async Task<(byte[] bytes, string fileName, string contentType)> GenerateFixedEmployeeFile(FileDownloadRequest request)
    {
        // Get record count from parameters, default 20
        var count = 20;
        if (request.Parameters?.TryGetValue("count", out var countStr) == true)
        {
            int.TryParse(countStr, out count);
            count = Math.Max(1, Math.Min(count, 1000)); // Limit 1-1000
        }

        var employees = SampleDataGenerator.GenerateEmployees(count);
        
        using var stream = new MemoryStream();
        await _fixedLengthService.WriteStreamAsync(stream, employees);
        
        var fileName = $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        return (stream.ToArray(), fileName, "text/plain");
    }

    /// <summary>
    /// Generate Transaction fixed length file
    /// </summary>
    private async Task<(byte[] bytes, string fileName, string contentType)> GenerateFixedTransactionFile(FileDownloadRequest request)
    {
        var count = 30;
        if (request.Parameters?.TryGetValue("count", out var countStr) == true)
        {
            int.TryParse(countStr, out count);
            count = Math.Max(1, Math.Min(count, 1000));
        }

        var transactions = SampleDataGenerator.GenerateTransactions(count);
        
        using var stream = new MemoryStream();
        await _fixedLengthService.WriteStreamAsync(stream, transactions);
        
        var fileName = $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        return (stream.ToArray(), fileName, "text/plain");
    }

    /// <summary>
    /// Generate Invoice fixed length file
    /// </summary>
    private async Task<(byte[] bytes, string fileName, string contentType)> GenerateFixedInvoiceFile(FileDownloadRequest request)
    {
        var count = 15;
        if (request.Parameters?.TryGetValue("count", out var countStr) == true)
        {
            int.TryParse(countStr, out count);
            count = Math.Max(1, Math.Min(count, 1000));
        }

        var invoices = SampleDataGenerator.GenerateInvoices(count);
        
        using var stream = new MemoryStream();
        await _fixedLengthService.WriteStreamAsync(stream, invoices);
        
        var fileName = $"invoices_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        return (stream.ToArray(), fileName, "text/plain");
    }

    #endregion

    #region Sample File Generators

    private static (byte[] bytes, string fileName, string contentType) GenerateSamplePdf()
    {
        // Demo: Simple PDF content (not real PDF format, just for demo)
        var content = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\nxref\n0 4\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\n0000000115 00000 n\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n196\n%%EOF";
        return (Encoding.UTF8.GetBytes(content), "sample.pdf", "application/pdf");
    }

    private static (byte[] bytes, string fileName, string contentType) GenerateSampleCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("ID,Name,Email,Age");
        csv.AppendLine("1,John Doe,john@example.com,30");
        csv.AppendLine("2,Jane Smith,jane@example.com,25");
        csv.AppendLine("3,Bob Johnson,bob@example.com,35");
        
        return (Encoding.UTF8.GetBytes(csv.ToString()), "sample.csv", "text/csv");
    }

    private static (byte[] bytes, string fileName, string contentType) GenerateSampleText()
    {
        var text = $@"Sample Text File
Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

This is a demo text file for download testing.

Features:
- Single file download
- Multiple files download (ZIP)
- Custom file generation

Thank you for testing!";
        
        return (Encoding.UTF8.GetBytes(text), "sample.txt", "text/plain");
    }

    private static (byte[] bytes, string fileName, string contentType) GenerateSampleJson()
    {
        var json = @"{
  ""title"": ""Sample JSON File"",
  ""timestamp"": """ + DateTime.Now.ToString("o") + @""",
  ""data"": {
    ""items"": [
      { ""id"": 1, ""name"": ""Item 1"" },
      { ""id"": 2, ""name"": ""Item 2"" },
      { ""id"": 3, ""name"": ""Item 3"" }
    ]
  },
  ""metadata"": {
    ""version"": ""1.0"",
    ""format"": ""demo""
  }
}";
        
        return (Encoding.UTF8.GetBytes(json), "sample.json", "application/json");
    }

    private static (byte[] bytes, string fileName, string contentType) GenerateDefaultFile(string fileId)
    {
        var content = $"Default file content for: {fileId}\nGenerated at: {DateTime.Now}";
        return (Encoding.UTF8.GetBytes(content), $"{fileId}.txt", "text/plain");
    }

    #endregion
}
