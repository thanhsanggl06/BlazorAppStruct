CREATE OR ALTER PROCEDURE dbo.usp_Todo_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Id], [Title], [IsDone], [CreatedAt]
    FROM [dbo].[TodoItems]
    ORDER BY [CreatedAt] DESC, [Id] DESC;
END
