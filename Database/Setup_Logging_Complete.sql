-- =============================================
-- Complete Logging System Setup Script
-- Run this script to setup entire logging infrastructure
-- =============================================

USE [BlazorWasmHostedDb];
GO

PRINT '============================================='
PRINT 'Starting Logging System Setup'
PRINT '============================================='
GO

-- =============================================
-- 1. CREATE TABLE
-- =============================================
PRINT ''
PRINT 'Step 1: Creating ApplicationLogs table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApplicationLogs')
BEGIN
    CREATE TABLE [dbo].[ApplicationLogs]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY CLUSTERED,
        [Timestamp] DATETIME2(7) NOT NULL,
        [Level] NVARCHAR(50) NOT NULL,
        [Message] NVARCHAR(MAX) NULL,
        [MessageTemplate] NVARCHAR(MAX) NULL,
        [Exception] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [LogEvent] NVARCHAR(MAX) NULL,
        
        -- Custom enriched properties for better querying
        [CorrelationId] NVARCHAR(100) NULL,
        [RequestPath] NVARCHAR(500) NULL,
        [RequestMethod] NVARCHAR(20) NULL,
        [SourceContext] NVARCHAR(500) NULL,
        [MachineName] NVARCHAR(100) NULL,
        [EnvironmentName] NVARCHAR(50) NULL,
        [ApplicationName] NVARCHAR(100) NULL,
        [ThreadId] INT NULL,
        [ProcessId] INT NULL,
        
        CONSTRAINT [DF_ApplicationLogs_Timestamp] DEFAULT (GETUTCDATE()) FOR [Timestamp]
    );

    -- Index for common queries
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Timestamp] 
        ON [dbo].[ApplicationLogs] ([Timestamp] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level] 
        ON [dbo].[ApplicationLogs] ([Level]) 
        INCLUDE ([Timestamp], [Message]);
    
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_CorrelationId] 
        ON [dbo].[ApplicationLogs] ([CorrelationId]) 
        WHERE [CorrelationId] IS NOT NULL;
    
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_SourceContext] 
        ON [dbo].[ApplicationLogs] ([SourceContext]) 
        WHERE [SourceContext] IS NOT NULL;

    PRINT '? Table ApplicationLogs created successfully with indexes.';
END
ELSE
BEGIN
    PRINT '! Table ApplicationLogs already exists.';
END
GO

-- =============================================
-- 2. CREATE STORED PROCEDURES
-- =============================================
PRINT ''
PRINT 'Step 2: Creating stored procedures...'
GO

-- usp_Logs_GetByTimeRange
PRINT '  Creating usp_Logs_GetByTimeRange...'
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_Logs_GetByTimeRange]
    @StartTime DATETIME2(7),
    @EndTime DATETIME2(7),
    @Level NVARCHAR(50) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        [Id], [Timestamp], [Level], [Message], [MessageTemplate], [Exception],
        [CorrelationId], [RequestPath], [RequestMethod], [SourceContext],
        [MachineName], [EnvironmentName], [ApplicationName], [ThreadId], [ProcessId]
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime 
        AND [Timestamp] <= @EndTime
        AND (@Level IS NULL OR [Level] = @Level)
    ORDER BY [Timestamp] DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) as TotalCount
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime 
        AND [Timestamp] <= @EndTime
        AND (@Level IS NULL OR [Level] = @Level);
END
GO
PRINT '? usp_Logs_GetByTimeRange created'
GO

-- usp_Logs_GetByCorrelationId
PRINT '  Creating usp_Logs_GetByCorrelationId...'
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_Logs_GetByCorrelationId]
    @CorrelationId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        [Id], [Timestamp], [Level], [Message], [MessageTemplate], [Exception],
        [CorrelationId], [RequestPath], [RequestMethod], [SourceContext],
        [MachineName], [EnvironmentName], [ApplicationName], [ThreadId], [ProcessId], [Properties]
    FROM [dbo].[ApplicationLogs]
    WHERE [CorrelationId] = @CorrelationId
    ORDER BY [Timestamp] ASC;
END
GO
PRINT '? usp_Logs_GetByCorrelationId created'
GO

-- usp_Logs_GetErrors
PRINT '  Creating usp_Logs_GetErrors...'
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_Logs_GetErrors]
    @StartTime DATETIME2(7) = NULL,
    @EndTime DATETIME2(7) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartTime IS NULL SET @StartTime = DATEADD(HOUR, -24, GETUTCDATE());
    IF @EndTime IS NULL SET @EndTime = GETUTCDATE();

    SELECT 
        [Id], [Timestamp], [Level], [Message], [MessageTemplate], [Exception],
        [CorrelationId], [RequestPath], [RequestMethod], [SourceContext],
        [MachineName], [EnvironmentName], [ApplicationName], [Properties]
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime 
        AND [Timestamp] <= @EndTime
        AND [Level] IN ('Error', 'Fatal')
    ORDER BY [Timestamp] DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*) as TotalCount
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime 
        AND [Timestamp] <= @EndTime
        AND [Level] IN ('Error', 'Fatal');
END
GO
PRINT '? usp_Logs_GetErrors created'
GO

-- usp_Logs_GetStatistics
PRINT '  Creating usp_Logs_GetStatistics...'
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_Logs_GetStatistics]
    @StartTime DATETIME2(7) = NULL,
    @EndTime DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartTime IS NULL SET @StartTime = DATEADD(HOUR, -24, GETUTCDATE());
    IF @EndTime IS NULL SET @EndTime = GETUTCDATE();

    -- Statistics by level
    SELECT 
        [Level], COUNT(*) as [Count],
        MIN([Timestamp]) as [FirstOccurrence],
        MAX([Timestamp]) as [LastOccurrence]
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime AND [Timestamp] <= @EndTime
    GROUP BY [Level]
    ORDER BY [Count] DESC;

    -- Top error sources
    SELECT TOP 10
        [SourceContext], COUNT(*) as [ErrorCount]
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime 
        AND [Timestamp] <= @EndTime
        AND [Level] IN ('Error', 'Fatal')
    GROUP BY [SourceContext]
    ORDER BY [ErrorCount] DESC;

    -- Hourly distribution
    SELECT 
        DATEPART(HOUR, [Timestamp]) as [Hour],
        [Level], COUNT(*) as [Count]
    FROM [dbo].[ApplicationLogs]
    WHERE [Timestamp] >= @StartTime AND [Timestamp] <= @EndTime
    GROUP BY DATEPART(HOUR, [Timestamp]), [Level]
    ORDER BY [Hour], [Level];
END
GO
PRINT '? usp_Logs_GetStatistics created'
GO

-- usp_Logs_Cleanup
PRINT '  Creating usp_Logs_Cleanup...'
GO
CREATE OR ALTER PROCEDURE [dbo].[usp_Logs_Cleanup]
    @RetentionDays INT = 90,
    @BatchSize INT = 10000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedCount INT = 0;
    DECLARE @TotalDeleted INT = 0;
    DECLARE @CutoffDate DATETIME2(7);
    
    SET @CutoffDate = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    
    PRINT 'Starting cleanup of logs older than ' + CAST(@CutoffDate AS NVARCHAR(50));
    
    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize)
        FROM [dbo].[ApplicationLogs]
        WHERE [Timestamp] < @CutoffDate;
        
        SET @DeletedCount = @@ROWCOUNT;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        
        IF @DeletedCount = 0 BREAK;
            
        PRINT 'Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' records...';
        WAITFOR DELAY '00:00:01';
    END
    
    PRINT 'Cleanup completed. Total deleted: ' + CAST(@TotalDeleted AS NVARCHAR(10)) + ' records';
    SELECT @TotalDeleted as TotalDeleted, @CutoffDate as CutoffDate;
END
GO
PRINT '? usp_Logs_Cleanup created'
GO

-- =============================================
-- 3. VERIFICATION
-- =============================================
PRINT ''
PRINT 'Step 3: Verifying installation...'
GO

-- Check table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ApplicationLogs')
    PRINT '? Table: ApplicationLogs exists'
ELSE
    PRINT '? ERROR: Table ApplicationLogs not found!'
GO

-- Check indexes
DECLARE @IndexCount INT;
SELECT @IndexCount = COUNT(*) 
FROM sys.indexes 
WHERE object_id = OBJECT_ID('ApplicationLogs') AND name IS NOT NULL;

PRINT '? Indexes: ' + CAST(@IndexCount AS NVARCHAR(10)) + ' indexes created'
GO

-- Check procedures
DECLARE @ProcCount INT;
SELECT @ProcCount = COUNT(*) 
FROM sys.procedures 
WHERE name LIKE 'usp_Logs_%';

PRINT '? Stored Procedures: ' + CAST(@ProcCount AS NVARCHAR(10)) + ' procedures created'
GO

-- =============================================
-- 4. TEST DATA (Optional)
-- =============================================
PRINT ''
PRINT 'Step 4: Inserting test data...'
GO

INSERT INTO [dbo].[ApplicationLogs] 
    ([Timestamp], [Level], [Message], [CorrelationId], [RequestPath], [SourceContext], [MachineName], [ApplicationName])
VALUES 
    (GETUTCDATE(), 'Information', 'Logging system setup completed', NEWID(), '/api/setup', 'Setup.Script', HOST_NAME(), 'BlazorAppStruct'),
    (GETUTCDATE(), 'Information', 'Test log entry created', NEWID(), '/api/test', 'Setup.Script', HOST_NAME(), 'BlazorAppStruct');

PRINT '? Test data inserted'
GO

-- =============================================
-- 5. COMPLETION
-- =============================================
PRINT ''
PRINT '============================================='
PRINT 'Logging System Setup COMPLETED!'
PRINT '============================================='
PRINT ''
PRINT 'Next steps:'
PRINT '1. Run your application'
PRINT '2. Logs will appear in:'
PRINT '   - Console output'
PRINT '   - Server/Logs/log-YYYYMMDD.txt'
PRINT '   - ApplicationLogs table'
PRINT ''
PRINT 'Test queries:'
PRINT '  EXEC usp_Logs_GetErrors'
PRINT '  SELECT TOP 10 * FROM ApplicationLogs ORDER BY Timestamp DESC'
PRINT ''
PRINT 'Optional: Setup SQL Agent Job'
PRINT '  Run: Database/Jobs/Create_LogCleanup_Job.sql'
PRINT '============================================='
GO
