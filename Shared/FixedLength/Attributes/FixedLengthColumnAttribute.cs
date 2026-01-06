namespace Shared.FixedLength.Attributes;

/// <summary>
/// Ð?nh ngh?a m?t c?t trong file fixed length
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FixedLengthColumnAttribute : Attribute
{
    /// <summary>
    /// Th? t? c?t (b?t ð?u t? 1)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Ð? dài c? ð?nh c?a c?t (s? k? t?)
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// K? t? padding (m?c ð?nh là kho?ng tr?ng ' ')
    /// </summary>
    public char PadChar { get; set; } = ' ';

    /// <summary>
    /// V? trí padding: Left ho?c Right (m?c ð?nh Right cho text, Left cho s?)
    /// </summary>
    public PaddingDirection Padding { get; set; } = PaddingDirection.Right;

    /// <summary>
    /// Format string cho DateTime, Decimal, etc.
    /// Ví d?: "yyyyMMdd", "000000.00"
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Có b? qua kho?ng tr?ng khi ð?c không (Trim)
    /// </summary>
    public bool TrimOnRead { get; set; } = true;

    /// <summary>
    /// Giá tr? m?c ð?nh n?u null
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Type c?a custom converter (ph?i implement IFixedLengthConverter)
    /// </summary>
    public Type? ConverterType { get; set; }
}

/// <summary>
/// Hý?ng padding
/// </summary>
public enum PaddingDirection
{
    /// <summary>
    /// Padding bên ph?i (text align left): "ABC   "
    /// </summary>
    Right,

    /// <summary>
    /// Padding bên trái (text align right): "   ABC"
    /// </summary>
    Left
}
