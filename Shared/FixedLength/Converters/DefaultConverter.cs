using System.Globalization;

namespace Shared.FixedLength.Converters;

/// <summary>
/// Default converter cho các ki?u d? li?u cõ b?n
/// </summary>
public class DefaultConverter : IFixedLengthConverter
{
    public string? ConvertToString(object? value, int length, string? format)
    {
        if (value == null)
            return null;

        return value switch
        {
            DateTime dt => dt.ToString(format ?? "yyyyMMdd", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString(format ?? "yyyyMMdd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString(format ?? "HHmmss", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(format ?? "F2", CultureInfo.InvariantCulture).Replace(".", "").Replace(",", ""),
            double dbl => dbl.ToString(format ?? "F2", CultureInfo.InvariantCulture).Replace(".", "").Replace(",", ""),
            float flt => flt.ToString(format ?? "F2", CultureInfo.InvariantCulture).Replace(".", "").Replace(",", ""),
            int i => i.ToString(format ?? "D", CultureInfo.InvariantCulture),
            long l => l.ToString(format ?? "D", CultureInfo.InvariantCulture),
            bool b => format ?? (b ? "Y" : "N"),
            _ => value.ToString()
        };
    }

    public object? ConvertFromString(string value, Type targetType, string? format)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GetDefaultValue(targetType);

        try
        {
            // X? l? Nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(DateTime))
            {
                return DateTime.ParseExact(value, format ?? "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(DateOnly))
            {
                return DateOnly.ParseExact(value, format ?? "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(TimeOnly))
            {
                return TimeOnly.ParseExact(value, format ?? "HHmmss", CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(decimal))
            {
                // N?u format có ch?a s? ch? s? th?p phân, c?n insert d?u ch?m
                if (!string.IsNullOrEmpty(format) && format.Contains('.'))
                {
                    var decimalPlaces = format.Split('.')[1].Length;
                    if (value.Length >= decimalPlaces)
                    {
                        var insertPos = value.Length - decimalPlaces;
                        value = value.Insert(insertPos, ".");
                    }
                }
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(double))
            {
                return double.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(float))
            {
                return float.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(int))
            {
                return int.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(long))
            {
                return long.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (underlyingType == typeof(bool))
            {
                var checkValue = format ?? "Y";
                return value.Equals(checkValue, StringComparison.OrdinalIgnoreCase);
            }
            else if (underlyingType == typeof(string))
            {
                return value;
            }

            // Fallback: try Convert.ChangeType
            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
