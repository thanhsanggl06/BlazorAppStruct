using System.Reflection;
using System.Text;
using Shared.FixedLength.Attributes;
using Shared.FixedLength.Converters;

namespace Shared.FixedLength.Services;

/// <summary>
/// Service x? l? ð?c/ghi file fixed length
/// </summary>
public class FixedLengthFileService
{
    private readonly DefaultConverter _defaultConverter = new();

    /// <summary>
    /// Ghi danh sách object ra file fixed length
    /// </summary>
    public async Task WriteFileAsync<T>(string filePath, IEnumerable<T> records, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var lines = records.Select(r => ConvertToLine(r)).ToList();
        await File.WriteAllLinesAsync(filePath, lines, encoding);
    }

    /// <summary>
    /// Ghi danh sách object ra stream
    /// </summary>
    public async Task WriteStreamAsync<T>(Stream stream, IEnumerable<T> records, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var writer = new StreamWriter(stream, encoding, leaveOpen: true);
        foreach (var record in records)
        {
            var line = ConvertToLine(record);
            await writer.WriteLineAsync(line);
        }
    }

    /// <summary>
    /// Ð?c file fixed length thành danh sách object
    /// </summary>
    public async Task<List<T>> ReadFileAsync<T>(string filePath, Encoding? encoding = null) where T : new()
    {
        encoding ??= Encoding.UTF8;
        var lines = await File.ReadAllLinesAsync(filePath, encoding);
        return lines.Select(line => ConvertFromLine<T>(line)).ToList();
    }

    /// <summary>
    /// Ð?c stream thành danh sách object
    /// </summary>
    public async Task<List<T>> ReadStreamAsync<T>(Stream stream, Encoding? encoding = null) where T : new()
    {
        encoding ??= Encoding.UTF8;
        var result = new List<T>();
        using var reader = new StreamReader(stream, encoding, leaveOpen: true);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            result.Add(ConvertFromLine<T>(line));
        }
        
        return result;
    }

    /// <summary>
    /// Convert m?t object thành d?ng text fixed length
    /// </summary>
    public string ConvertToLine<T>(T record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        var type = typeof(T);
        var properties = GetOrderedProperties(type);
        var sb = new StringBuilder();

        foreach (var (property, attribute) in properties)
        {
            var value = property.GetValue(record);
            var stringValue = ConvertValueToString(value, attribute, property.PropertyType);
            var paddedValue = ApplyPadding(stringValue, attribute);
            sb.Append(paddedValue);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert d?ng text fixed length thành object
    /// </summary>
    public T ConvertFromLine<T>(string line) where T : new()
    {
        if (string.IsNullOrEmpty(line))
            throw new ArgumentException("Line cannot be null or empty", nameof(line));

        var result = new T();
        var type = typeof(T);
        var properties = GetOrderedProperties(type);
        var position = 0;

        foreach (var (property, attribute) in properties)
        {
            if (position + attribute.Length > line.Length)
            {
                // D?ng b? thi?u d? li?u, dùng default value
                var defaultValue = attribute.DefaultValue ?? GetDefaultValue(property.PropertyType);
                property.SetValue(result, defaultValue);
                continue;
            }

            var stringValue = line.Substring(position, attribute.Length);
            
            if (attribute.TrimOnRead)
                stringValue = stringValue.Trim();

            var value = ConvertStringToValue(stringValue, attribute, property.PropertyType);
            property.SetValue(result, value);
            
            position += attribute.Length;
        }

        return result;
    }

    #region Private Helper Methods

    private List<(PropertyInfo Property, FixedLengthColumnAttribute Attribute)> GetOrderedProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<FixedLengthColumnAttribute>()
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute!.Order)
            .Select(x => (x.Property, x.Attribute!))
            .ToList();

        return properties;
    }

    private string ConvertValueToString(object? value, FixedLengthColumnAttribute attribute, Type propertyType)
    {
        // N?u value null, dùng default value
        if (value == null)
        {
            value = attribute.DefaultValue ?? GetDefaultValue(propertyType);
        }

        // N?u có custom converter
        if (attribute.ConverterType != null)
        {
            var converter = CreateConverter(attribute.ConverterType);
            return converter.ConvertToString(value, attribute.Length, attribute.Format) ?? string.Empty;
        }

        // Dùng default converter
        return _defaultConverter.ConvertToString(value, attribute.Length, attribute.Format) ?? string.Empty;
    }

    private object? ConvertStringToValue(string stringValue, FixedLengthColumnAttribute attribute, Type propertyType)
    {
        // N?u có custom converter
        if (attribute.ConverterType != null)
        {
            var converter = CreateConverter(attribute.ConverterType);
            return converter.ConvertFromString(stringValue, propertyType, attribute.Format);
        }

        // Dùng default converter
        return _defaultConverter.ConvertFromString(stringValue, propertyType, attribute.Format);
    }

    private string ApplyPadding(string value, FixedLengthColumnAttribute attribute)
    {
        if (value.Length > attribute.Length)
        {
            // Truncate n?u quá dài
            return value.Substring(0, attribute.Length);
        }
        else if (value.Length < attribute.Length)
        {
            // Padding
            var paddingLength = attribute.Length - value.Length;
            var padding = new string(attribute.PadChar, paddingLength);
            
            return attribute.Padding == PaddingDirection.Right
                ? value + padding
                : padding + value;
        }

        return value;
    }

    private IFixedLengthConverter CreateConverter(Type converterType)
    {
        if (!typeof(IFixedLengthConverter).IsAssignableFrom(converterType))
        {
            throw new InvalidOperationException(
                $"Converter type {converterType.Name} must implement IFixedLengthConverter");
        }

        return (IFixedLengthConverter)(Activator.CreateInstance(converterType) 
            ?? throw new InvalidOperationException($"Cannot create instance of {converterType.Name}"));
    }

    private object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    #endregion
}
