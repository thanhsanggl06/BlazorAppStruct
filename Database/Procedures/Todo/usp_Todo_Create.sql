CREATE OR ALTER PROCEDURE dbo.usp_Todo_Create
    @Title NVARCHAR(200),
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[TodoItems]([Title], [IsDone], [CreatedAt])
    VALUES (@Title, 0, SYSUTCDATETIME());

    SET @Id = SCOPE_IDENTITY();

    SELECT [Id], [Title], [IsDone], [CreatedAt]
    FROM [dbo].[TodoItems]
    WHERE [Id] = @Id;
END
