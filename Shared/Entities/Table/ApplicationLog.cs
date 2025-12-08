namespace Shared.Entities.Table;

/// <summary>
/// Entity for application logs stored in database
/// </summary>
public class ApplicationLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public string? LogEvent { get; set; }
    
    // Custom enriched properties
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? SourceContext { get; set; }
    public string? MachineName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ApplicationName { get; set; }
    public int? ThreadId { get; set; }
    public int? ProcessId { get; set; }
}
