using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Data.Dapper.Infrastructure;

/// <summary>
/// Custom column name mapper cho Dapper
/// T? ð?ng map snake_case (database) ? PascalCase (C#)
/// </summary>
public class DapperSnakeCaseMapper : SqlMapper.ITypeMap
{
    private readonly Type _type;

    public DapperSnakeCaseMapper(Type type)
    {
        _type = type;
    }

    public ConstructorInfo? FindConstructor(string[] names, Type[] types)
    {
        // S? d?ng default constructor
        return _type.GetConstructor(Type.EmptyTypes);
    }

    public ConstructorInfo? FindExplicitConstructor()
    {
        return null;
    }

    public SqlMapper.IMemberMap? GetConstructorParameter(ConstructorInfo constructor, string columnName)
    {
        return null;
    }

    public SqlMapper.IMemberMap? GetMember(string columnName)
    {
        // Convert snake_case ? PascalCase
        var propertyName = ConvertSnakeCaseToPascalCase(columnName);

        var property = _type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        
        if (property != null)
        {
            return new SimpleMemberMap(columnName, property);
        }

        // Fallback: Try exact match
        property = _type.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance);
        
        if (property != null)
        {
            return new SimpleMemberMap(columnName, property);
        }

        return null;
    }

    private static string ConvertSnakeCaseToPascalCase(string snakeCase)
    {
        if (string.IsNullOrWhiteSpace(snakeCase))
            return snakeCase;

        var parts = snakeCase.Split('_');
        var result = string.Join("", parts.Select(part => 
            char.ToUpper(part[0]) + part.Substring(1).ToLower()
        ));

        return result;
    }

    private class SimpleMemberMap : SqlMapper.IMemberMap
    {
        private readonly string _columnName;
        private readonly PropertyInfo _property;

        public SimpleMemberMap(string columnName, PropertyInfo property)
        {
            _columnName = columnName;
            _property = property;
        }

        public string ColumnName => _columnName;

        public Type MemberType => _property.PropertyType;

        public PropertyInfo? Property => _property;

        public FieldInfo? Field => null;

        public ParameterInfo? Parameter => null;
    }
}

/// <summary>
/// Extension methods ð? register mapper
/// </summary>
public static class DapperMapperExtensions
{
    /// <summary>
    /// Ðãng k? snake_case mapper cho m?t type
    /// </summary>
    public static void RegisterSnakeCaseMapper<T>()
    {
        SqlMapper.SetTypeMap(typeof(T), new DapperSnakeCaseMapper(typeof(T)));
    }

    /// <summary>
    /// Ðãng k? snake_case mapper cho nhi?u types
    /// </summary>
    public static void RegisterSnakeCaseMappers(params Type[] types)
    {
        foreach (var type in types)
        {
            SqlMapper.SetTypeMap(type, new DapperSnakeCaseMapper(type));
        }
    }

    /// <summary>
    /// ? GLOBAL: Ðãng k? default type mapper cho T?T C? types
    /// T? ð?ng áp d?ng snake_case mapping cho m?i DTO
    /// </summary>
    public static void RegisterGlobalSnakeCaseMapper()
    {
        // Set default type map factory
        SqlMapper.TypeMapProvider = type =>
        {
            // N?u type chýa có mapper, t?o snake_case mapper
            return new DapperSnakeCaseMapper(type);
        };
    }
}
