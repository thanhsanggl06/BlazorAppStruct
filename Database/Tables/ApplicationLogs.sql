-- =============================================
-- Application Logs Table
-- Stores structured logs from Serilog
-- =============================================

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

    PRINT 'Table ApplicationLogs created successfully with indexes.';
END
ELSE
BEGIN
    PRINT 'Table ApplicationLogs already exists.';
END
GO
