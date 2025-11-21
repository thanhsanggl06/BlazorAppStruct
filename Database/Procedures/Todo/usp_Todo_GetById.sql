CREATE OR ALTER PROCEDURE dbo.usp_Todo_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Id], [Title], [IsDone], [CreatedAt]
    FROM [dbo].[TodoItems]
    WHERE [Id] = @Id;
END
