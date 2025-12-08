# Enterprise Logging System - Setup Guide

## Overview

H? th?ng logging chu?n enterprise cho BlazorAppStruct s? d?ng **Serilog** v?i các tính nãng:

- ? **File Logging**: Rolling files theo ngày, t? ð?ng rotate
- ? **Database Logging**: Ghi vào SQL Server v?i schema tùy ch?nh
- ? **Structured Logging**: JSON properties cho query d? dàng
- ? **Correlation ID**: Tracking requests qua toàn b? pipeline
- ? **Environment-based Config**: C?u h?nh khác nhau cho Dev/Prod
- ? **Auto Cleanup**: SQL Agent job t? ð?ng xóa logs c?
- ? **Monitoring Queries**: Dashboard queries s?n sàng

## Architecture

```
Request ? CorrelationIdMiddleware ? SerilogRequestLogging ? Application
                ?                           ?                      ?
          Enrich Logs              HTTP Request Logs      Business Logs
                ?                           ?                      ?
                ????????????????????????????????????????????????????
                                            ?
                        ?????????????????????????????????????????
                        ?                                       ?
                  Console Output                          File Output
                                                               ?
                                                    Logs/log-YYYYMMDD.txt
                                                               ?
                                                      Database Output
                                                               ?
                                                    ApplicationLogs Table
```

## Installation Steps

### 1. Database Setup

Ch?y các scripts SQL theo th? t?:

```sql
-- 1. T?o b?ng ApplicationLogs
Database/Tables/ApplicationLogs.sql

-- 2. T?o các stored procedures
Database/Procedures/Logs/usp_Logs_GetByTimeRange.sql
Database/Procedures/Logs/usp_Logs_GetByCorrelationId.sql
Database/Procedures/Logs/usp_Logs_GetErrors.sql
Database/Procedures/Logs/usp_Logs_GetStatistics.sql
Database/Procedures/Logs/usp_Logs_Cleanup.sql

-- 3. T?o SQL Agent Job (optional, n?u mu?n auto cleanup)
Database/Jobs/Create_LogCleanup_Job.sql
```

### 2. Configuration

File `appsettings.json` ð? ðý?c c?u h?nh v?i:

#### Console Sink
- Output template v?i colors
- Production level: Information

#### File Sink
- Rolling theo ngày: `Logs/log-YYYYMMDD.txt`
- Max file size: 10MB per file
- Retention: 30 files (30 days)
- Shared mode: Multiple processes có th? write

#### Database Sink
- Table: `ApplicationLogs`
- Auto-create table: Disabled (use SQL script)
- Minimum level: Information
- Custom columns cho easy querying

### 3. Environment-Specific Config

**Development** (`appsettings.Development.json`):
- Log level: Debug
- Verbose console output
- Shorter retention (7 days)
- Includes full properties in file logs

**Production** (`appsettings.json`):
- Log level: Information
- Minimal console output
- Longer retention (30 days)
- Optimized for performance

## Usage Examples

### 1. Basic Logging

```csharp
public class TodoService
{
    private readonly ILogger<TodoService> _logger;
    
    public TodoService(ILogger<TodoService> logger)
    {
        _logger = logger;
    }
    
    public async Task<TodoItem> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching todo item with ID: {TodoId}", id);
        
        try
        {
            var item = await _repository.GetByIdAsync(id);
            
            if (item == null)
            {
                _logger.LogWarning("Todo item not found: {TodoId}", id);
                return null;
            }
            
            _logger.LogDebug("Successfully retrieved todo: {@TodoItem}", item);
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching todo item {TodoId}", id);
            throw;
        }
    }
}
```

### 2. Structured Logging v?i Properties

```csharp
_logger.LogInformation(
    "User {UserId} created todo {TodoId} with title {Title}",
    userId, todoId, title
);

// Properties ðý?c lýu trong database và có th? query
```

### 3. Logging Complex Objects

```csharp
// Use @ prefix ð? serialize object
_logger.LogInformation("Processing request: {@Request}", request);

// Use $ prefix ð? call ToString()
_logger.LogInformation("Processing request: {$Request}", request);
```

### 4. Using Correlation ID

Correlation ID ðý?c t? ð?ng thêm vào m?i request. Ð? l?y ra:

```csharp
// From HTTP context
var correlationId = HttpContext.Response.Headers["X-Correlation-ID"].FirstOrDefault();

// Ho?c enrich manually
using (LogContext.PushProperty("OrderId", orderId))
{
    _logger.LogInformation("Processing order");
    // All logs in this scope will have OrderId property
}
```

## Querying Logs

### 1. Using Stored Procedures

```sql
-- Get logs by time range
EXEC usp_Logs_GetByTimeRange 
    @StartTime = '2024-01-01', 
    @EndTime = '2024-01-02',
    @Level = 'Error',
    @PageNumber = 1,
    @PageSize = 50;

-- Get all logs for a request
EXEC usp_Logs_GetByCorrelationId 
    @CorrelationId = '12345678-1234-1234-1234-123456789012';

-- Get error summary
EXEC usp_Logs_GetErrors 
    @StartTime = '2024-01-01',
    @PageNumber = 1,
    @PageSize = 100;

-- Get statistics
EXEC usp_Logs_GetStatistics;
```

### 2. Using Dashboard Queries

Ch?y file `Database/Queries/Log_Monitoring_Dashboard.sql` ð? xem:
- Recent errors
- Log level distribution
- Top error sources
- Request paths with most errors
- Hourly log volume
- Exception types
- And more...

### 3. Direct Queries

```sql
-- Find all errors in last hour
SELECT * FROM ApplicationLogs
WHERE Level = 'Error' 
    AND Timestamp >= DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY Timestamp DESC;

-- Find logs by source
SELECT * FROM ApplicationLogs
WHERE SourceContext LIKE '%TodoService%'
ORDER BY Timestamp DESC;

-- Track a specific request
SELECT * FROM ApplicationLogs
WHERE CorrelationId = 'your-correlation-id'
ORDER BY Timestamp ASC;
```

## Maintenance

### Auto Cleanup

SQL Agent Job ch?y hàng ngày lúc 2:00 AM ð? xóa logs > 90 ngày.

Ð? thay ð?i retention period:
```sql
-- Edit the job step command
EXEC [dbo].[usp_Logs_Cleanup] 
    @RetentionDays = 60,  -- Change this
    @BatchSize = 10000;
```

### Manual Cleanup

```sql
-- Cleanup logs older than 30 days
EXEC usp_Logs_Cleanup @RetentionDays = 30;

-- Or direct delete
DELETE FROM ApplicationLogs 
WHERE Timestamp < DATEADD(DAY, -30, GETUTCDATE());
```

### File Cleanup

Files trong `Logs/` folder t? ð?ng rotate và gi? t?i ða:
- **Production**: 30 files (c?u h?nh trong appsettings.json)
- **Development**: 7 files

## Performance Considerations

### Database Indexes

B?ng ApplicationLogs có các indexes:
- `IX_ApplicationLogs_Timestamp` - For time-based queries
- `IX_ApplicationLogs_Level` - For filtering by log level
- `IX_ApplicationLogs_CorrelationId` - For request tracking
- `IX_ApplicationLogs_SourceContext` - For filtering by source

### Optimization Tips

1. **Batch Cleanup**: Stored procedure `usp_Logs_Cleanup` delete theo batch ð? tránh lock
2. **Async Logging**: Serilog write async, không block application
3. **Retention Policy**: Cleanup old logs thý?ng xuyên
4. **Selective Logging**: Ch? log Information+ trong production

## Troubleshooting

### Logs không ghi vào database

1. Ki?m tra connection string trong appsettings.json
2. Verify table exists: `SELECT * FROM sys.tables WHERE name = 'ApplicationLogs'`
3. Check SQL Server permissions
4. Review console logs for Serilog errors

### File logs không t?o

1. Ki?m tra folder `Logs/` exists và có write permission
2. Review `fileSizeLimitBytes` và `retainedFileCountLimit`
3. Check `shared: true` setting n?u multiple processes

### Performance issues

1. Reduce log level trong production
2. Increase cleanup frequency
3. Add more indexes for your specific queries
4. Consider partitioning table n?u > 10 million records

## Best Practices

1. ? **Use structured logging** v?i placeholders, không string concatenation
   ```csharp
   // Good
   _logger.LogInformation("User {UserId} logged in", userId);
   
   // Bad
   _logger.LogInformation($"User {userId} logged in");
   ```

2. ? **Log exceptions with context**
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
   }
   ```

3. ? **Use appropriate log levels**
   - Debug: Development only
   - Information: Important events
   - Warning: Unexpected but handled
   - Error: Errors requiring attention
   - Fatal: Application crash

4. ? **Include correlation ID** trong all external requests
   ```csharp
   client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
   ```

5. ? **Monitor logs regularly** using dashboard queries

## Security Notes

- ?? **Never log sensitive data**: passwords, tokens, credit cards
- ?? **Sanitize user input** trý?c khi log
- ?? **Secure log files**: Set proper folder permissions
- ?? **Encrypt connection string** trong production
- ?? **Review retention policy** theo compliance requirements

## Support

For issues or questions:
- Check troubleshooting section
- Review Serilog documentation: https://serilog.net
- Check application console output for Serilog errors
