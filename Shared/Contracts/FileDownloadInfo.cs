namespace Shared.Contracts;

/// <summary>
/// Thông tin file download - dùng chung gi?a Client và Server
/// </summary>
public record FileDownloadInfo
{
    /// <summary>
    /// Tên file (bao g?m extension)
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME type c?a file (vd: application/pdf, image/png, text/csv)
    /// </summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>
    /// Kích thý?c file (bytes) - optional
    /// </summary>
    public long? FileSize { get; init; }

    /// <summary>
    /// Metadata b? sung (optional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request ð? download 1 file
/// </summary>
public record FileDownloadRequest
{
    /// <summary>
    /// ID ho?c path c?a file c?n download
    /// </summary>
    public required string FileIdentifier { get; init; }

    /// <summary>
    /// Tham s? b? sung (optional)
    /// </summary>
    public Dictionary<string, string>? Parameters { get; init; }
}

/// <summary>
/// Request ð? download nhi?u files
/// </summary>
public record MultipleFilesDownloadRequest
{
    /// <summary>
    /// Danh sách file identifiers c?n download
    /// </summary>
    public required List<string> FileIdentifiers { get; init; }

    /// <summary>
    /// Tên file ZIP (n?u download d?ng archive)
    /// </summary>
    public string? ZipFileName { get; init; }

    /// <summary>
    /// Tham s? b? sung (optional)
    /// </summary>
    public Dictionary<string, string>? Parameters { get; init; }
}
