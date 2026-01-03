using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Data.Dapper.Infrastructure;

/// <summary>
/// Dapper mapper ð?c [Column] attribute ð? map column names
/// </summary>
public class ColumnAttributeTypeMapper<T> : FallbackTypeMapper
{
    public ColumnAttributeTypeMapper()
        : base(new SqlMapper.ITypeMap[]
        {
            new CustomPropertyTypeMap(typeof(T), (type, columnName) =>
                type.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName)
                    ) ?? type.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance)
            ),
            new DefaultTypeMap(typeof(T))
        })
    {
    }
}

/// <summary>
/// Fallback type mapper - th? nhi?u mappers theo th? t?
/// </summary>
public class FallbackTypeMapper : SqlMapper.ITypeMap
{
    private readonly IEnumerable<SqlMapper.ITypeMap> _mappers;

    public FallbackTypeMapper(IEnumerable<SqlMapper.ITypeMap> mappers)
    {
        _mappers = mappers;
    }

    public ConstructorInfo? FindConstructor(string[] names, Type[] types)
    {
        foreach (var mapper in _mappers)
        {
            var result = mapper.FindConstructor(names, types);
            if (result != null) return result;
        }
        return null;
    }

    public ConstructorInfo? FindExplicitConstructor()
    {
        return _mappers
            .Select(m => m.FindExplicitConstructor())
            .FirstOrDefault(result => result != null);
    }

    public SqlMapper.IMemberMap? GetConstructorParameter(ConstructorInfo constructor, string columnName)
    {
        foreach (var mapper in _mappers)
        {
            var result = mapper.GetConstructorParameter(constructor, columnName);
            if (result != null) return result;
        }
        return null;
    }

    public SqlMapper.IMemberMap? GetMember(string columnName)
    {
        foreach (var mapper in _mappers)
        {
            var result = mapper.GetMember(columnName);
            if (result != null) return result;
        }
        return null;
    }
}

/// <summary>
/// Extension methods ð? register column attribute mapper
/// </summary>
public static class ColumnAttributeMapperExtensions
{
    /// <summary>
    /// Ðãng k? [Column] attribute mapper cho m?t type
    /// </summary>
    public static void RegisterColumnAttributeMapper<T>()
    {
        SqlMapper.SetTypeMap(typeof(T), new ColumnAttributeTypeMapper<T>());
    }

    /// <summary>
    /// Ðãng k? [Column] attribute mapper cho nhi?u types
    /// </summary>
    public static void RegisterColumnAttributeMappers(params Type[] types)
    {
        foreach (var type in types)
        {
            var mapperType = typeof(ColumnAttributeTypeMapper<>).MakeGenericType(type);
            var mapper = (SqlMapper.ITypeMap)Activator.CreateInstance(mapperType)!;
            SqlMapper.SetTypeMap(type, mapper);
        }
    }
}
