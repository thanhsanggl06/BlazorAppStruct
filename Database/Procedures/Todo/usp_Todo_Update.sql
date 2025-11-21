CREATE OR ALTER PROCEDURE dbo.usp_Todo_Update
    @Id INT,
    @Title NVARCHAR(200),
    @IsDone BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[TodoItems]
    SET [Title] = @Title,
        [IsDone] = @IsDone
    WHERE [Id] = @Id;

    IF @@ROWCOUNT = 0
    BEGIN
        -- Not found: return nothing
        RETURN;
    END

    SELECT [Id], [Title], [IsDone], [CreatedAt]
    FROM [dbo].[TodoItems]
    WHERE [Id] = @Id;
END
