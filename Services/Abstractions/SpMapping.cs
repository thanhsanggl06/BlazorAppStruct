using System.Reflection;

namespace Services.Abstractions;

public static class SpMapping
{
    public static T MapFromReader<T>(IDictionary<string, object?> row) where T : new()
    {
        var obj = new T();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            var key = row.Keys.FirstOrDefault(k => string.Equals(k, p.Name, StringComparison.OrdinalIgnoreCase));
            if (key is null) continue;
            var val = row[key];
            if (val is null || val is DBNull) continue;
            p.SetValue(obj, Convert.ChangeType(val, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
        }
        return obj;
    }
}
