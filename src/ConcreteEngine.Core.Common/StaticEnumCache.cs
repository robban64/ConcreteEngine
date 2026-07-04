namespace ConcreteEngine.Core.Common;

[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumExtAttribute : Attribute
{
    public EnumExtAttribute(){}
}


[AttributeUsage(AttributeTargets.Enum)]
public sealed class EnumCachedAttribute : Attribute
{
    public EnumCachedAttribute(){}
}

public static class StaticEnumCache
{
    private static readonly Dictionary<Type, string[]> EnumNames = new(8);
    
    public static int TotalCount => EnumNames.Count;
    public static int GetCount<T>() where T : Enum => EnumNames[typeof(T)].Length;

    public static ReadOnlySpan<string> GetNames<T>() where T : Enum => new(EnumNames[typeof(T)]);

    public static void Register(Type type, string[] names) => EnumNames.Add(type, names);

}