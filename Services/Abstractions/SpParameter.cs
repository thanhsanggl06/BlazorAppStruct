namespace Services.Abstractions;

public sealed record SpParameter(string Name, object? Value);

public static class SpParams
{
    public static object From(params SpParameter[] items)
        => items.ToDictionary(p => p.Name, p => p.Value);
}
