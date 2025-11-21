CREATE OR ALTER PROCEDURE dbo.usp_Todo_ListPaged
    @PageNumber INT = 1,
    @PageSize   INT = 20,
    @Search     NVARCHAR(200) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber <= 0 SET @PageNumber = 1;
    IF @PageSize <= 0 SET @PageSize = 20;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    ;WITH CTE AS (
        SELECT [Id], [Title], [IsDone], [CreatedAt]
        FROM [dbo].[TodoItems]
        WHERE (@Search IS NULL OR [Title] LIKE N'%' + @Search + N'%')
    )
    SELECT [Id], [Title], [IsDone], [CreatedAt]
    FROM CTE
    ORDER BY [CreatedAt] DESC, [Id] DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT @TotalCount = COUNT(*)
    FROM [dbo].[TodoItems]
    WHERE (@Search IS NULL OR [Title] LIKE N'%' + @Search + N'%');
END
