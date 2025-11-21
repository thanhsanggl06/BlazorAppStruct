CREATE OR ALTER PROCEDURE dbo.usp_Todo_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [dbo].[TodoItems]
    WHERE [Id] = @Id;

    SELECT @@ROWCOUNT AS Affected;
END
