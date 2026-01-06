using Shared.FixedLength.Converters;

namespace Shared.FixedLength.Models;

/// <summary>
/// Custom converter cho ð?nh d?ng s? ti?n VND
/// Ví d?: 1234567 -> "1.234.567ð" (khi ð?c), ngý?c l?i khi ghi
/// </summary>
public class VndCurrencyConverter : IFixedLengthConverter
{
    public string? ConvertToString(object? value, int length, string? format)
    {
        if (value == null)
            return null;

        // Chuy?n v? s? nguyên (b? ph?n th?p phân)
        var amount = value switch
        {
            decimal d => (long)d,
            double d => (long)d,
            float f => (long)f,
            int i => i,
            long l => l,
            _ => 0L
        };

        // Ch? tr? v? s?, không format d?u ch?m
        return amount.ToString().PadLeft(length, '0');
    }

    public object? ConvertFromString(string value, Type targetType, string? format)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        // Parse s? nguyên
        if (long.TryParse(value, out var amount))
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            if (underlyingType == typeof(decimal))
                return (decimal)amount;
            else if (underlyingType == typeof(long))
                return amount;
            else if (underlyingType == typeof(int))
                return (int)amount;
            else if (underlyingType == typeof(double))
                return (double)amount;
            else if (underlyingType == typeof(float))
                return (float)amount;
        }

        return 0m;
    }
}
