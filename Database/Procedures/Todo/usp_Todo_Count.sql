CREATE OR ALTER PROCEDURE dbo.usp_Todo_Count
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS TotalCount
    FROM [dbo].[TodoItems]
    WHERE (@Search IS NULL OR [Title] LIKE N'%' + @Search + N'%');
END
