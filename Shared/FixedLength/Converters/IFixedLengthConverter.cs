namespace Shared.FixedLength.Converters;

/// <summary>
/// Interface cho custom converter
/// </summary>
public interface IFixedLengthConverter
{
    /// <summary>
    /// Convert t? object sang string ð? ghi file
    /// </summary>
    string? ConvertToString(object? value, int length, string? format);

    /// <summary>
    /// Convert t? string sang object khi ð?c file
    /// </summary>
    object? ConvertFromString(string value, Type targetType, string? format);
}
