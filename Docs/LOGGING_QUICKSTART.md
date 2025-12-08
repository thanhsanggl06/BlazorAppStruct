# Logging Quick Start

## Setup trong 2 phút

### Bý?c 1: T?o b?ng (30 giây)

```sql
-- Ch?y file này trong SQL Server:
Database/Setup_Logging_Complete.sql
```

### Bý?c 2: Ch?y app (30 giây)

```bash
cd Server
dotnet run
```

**Xong!** Logs ðang ðý?c ghi vào:
- Console output (clean, không có noise)
- `Server/Logs/log-YYYYMMDD.txt` (chi ti?t)
- Database table `ApplicationLogs` (structured)

## Console Output M?u

### Development:
```
[14:30:45] [INF] Starting BlazorAppStruct application...
[14:30:46] [INF] Application started successfully on Development
[14:30:47] [INF] HTTP GET /api/todos ? 200 in 45.2341ms
    CorrelationId: abc-123-def | IP: 127.0.0.1 | GET /api/todos
[14:30:48] [WRN] HTTP GET /api/todos/999 ? 404 in 12.5432ms
    CorrelationId: xyz-456-abc | IP: 127.0.0.1 | GET /api/todos/999
```

### Production:
```
[14:30:45] [INF] Starting BlazorAppStruct application...
[14:30:46] [INF] Application started successfully on Production
[14:30:47] [INF] HTTP GET /api/todos ? 200 in 45.2341ms
```

**Lýu ?:** Static files (.wasm, .js, .css, _framework) không ðý?c log ð? gi?m noise!

## Xem logs

### Trong database:
```sql
-- 100 logs g?n nh?t v?i thông tin client
SELECT TOP 100 
    Timestamp,
    Level,
    Message,
    CorrelationId,
    RequestMethod,
    RequestPath,
    SourceContext
FROM ApplicationLogs 
ORDER BY Timestamp DESC;

-- Xem thông tin client chi ti?t
SELECT TOP 100 
    Timestamp,
    Message,
    JSON_VALUE(Properties, '$.RemoteIP') as ClientIP,
    JSON_VALUE(Properties, '$.UserAgent') as UserAgent,
    JSON_VALUE(Properties, '$.RequestHost') as Host
FROM ApplicationLogs 
WHERE RequestPath IS NOT NULL
ORDER BY Timestamp DESC;

-- Ch? errors
SELECT * FROM ApplicationLogs 
WHERE Level IN ('Error', 'Fatal') 
ORDER BY Timestamp DESC;
```

### Trong code:
```csharp
_logger.LogInformation("Processing {ItemId} from IP {IP}", id, remoteIp);

try {
    // code
} catch (Exception ex) {
    _logger.LogError(ex, "Failed {ItemId}", id);
}
```

## Log Levels Ðý?c Hi?n Th?

### Development:
- **Debug**: Internal tracing (không hi?n trong console)
- **Information**: API calls, business events
- **Warning**: 4xx errors (404, 400, etc)
- **Error**: 5xx errors, exceptions
- **Fatal**: Application crash

### Production:
- **Information**: Ch? API calls quan tr?ng
- **Warning**: 4xx errors
- **Error**: 5xx errors, exceptions
- **Fatal**: Application crash

## C?u h?nh Ð? Ðý?c L?c

### ? KHÔNG log (gi?m noise):
- `Microsoft.*` framework internals
- `System.*` system calls
- Entity Framework queries
- Static files (_.wasm, _.css, _.js)
- Health check endpoints
- _framework files

### ? CÓ log:
- API calls `/api/*`
- Application startup/shutdown
- Business logic trong services
- Errors và exceptions
- Custom logs t? code c?a b?n

## Properties Ðý?c Enriched

M?i log t? ð?ng có:
- **CorrelationId**: Track request flow
- **RemoteIP**: Client IP address
- **UserAgent**: Browser/client info
- **RequestHost**: Domain ðý?c g?i
- **RequestMethod**: GET, POST, etc
- **RequestPath**: URL path
- **SourceContext**: Class/service name
- **MachineName**: Server name
- **EnvironmentName**: Dev/Prod
- **ThreadId**: Thread ID
- **ProcessId**: Process ID

## Queries H?u Ích

```sql
-- Track 1 request hoàn ch?nh
SELECT * FROM ApplicationLogs 
WHERE CorrelationId = 'your-id' 
ORDER BY Timestamp;

-- API calls trong 1 gi? qua
SELECT * FROM ApplicationLogs 
WHERE RequestPath LIKE '/api/%'
  AND Timestamp >= DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY Timestamp DESC;

-- Errors theo IP
SELECT 
    JSON_VALUE(Properties, '$.RemoteIP') as ClientIP,
    COUNT(*) as ErrorCount
FROM ApplicationLogs 
WHERE Level = 'Error'
  AND Timestamp >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY JSON_VALUE(Properties, '$.RemoteIP')
ORDER BY ErrorCount DESC;

-- Slow requests (> 1 second)
SELECT * FROM ApplicationLogs
WHERE Message LIKE '%ms'
  AND CAST(SUBSTRING(Message, 
      CHARINDEX('in', Message) + 3,
      CHARINDEX('ms', Message) - CHARINDEX('in', Message) - 3) AS FLOAT) > 1000
ORDER BY Timestamp DESC;

-- Xóa logs c?
DELETE FROM ApplicationLogs 
WHERE Timestamp < DATEADD(DAY, -90, GETUTCDATE());
```

## Output Template Format

### Console (Development):
```
[HH:mm:ss] [LEVEL] Message
    CorrelationId: xxx | IP: xxx | METHOD PATH
```

### Console (Production):
```
[HH:mm:ss] [LEVEL] Message
```

### File (All):
```
[yyyy-MM-dd HH:mm:ss.fff] [LEVEL] Message
    CorrelationId: xxx | Source: xxx
    Request: METHOD PATH | IP: xxx
```

## Troubleshooting

**Quá nhi?u logs trong console?**
- ? Ð? fix: Static files không log
- ? Ð? fix: Framework internals b? filter
- N?u v?n nhi?u: Tãng `MinimumLevel` lên `Warning`

**Không th?y thông tin client?**
- ? Ð? fix: RemoteIP, UserAgent ðý?c enriched
- Xem trong file logs (chi ti?t hõn console)
- Ho?c query Properties column trong DB:
  ```sql
  SELECT Properties FROM ApplicationLogs WHERE Id = xxx;
  ```

**File logs quá l?n?**
- Retention: 30 days (Production), 7 days (Dev)
- Max size per file: 10MB
- T? ð?ng rotate m?i ngày

Chi ti?t: `Docs/LOGGING_GUIDE.md`
