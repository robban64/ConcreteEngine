namespace ConcreteEngine.Core.Common.Memory;

public static class EnumCache<T> where T : unmanaged, Enum
{
    public static readonly string EnumName = typeof(T).Name;
    private static readonly T[] Values = Enum.GetValues<T>();
    private static readonly string[] Names = Enum.GetNames<T>();

    public static ReadOnlySpan<T> GetValues() => Values;
    public static ReadOnlySpan<string> GetNames() => Names;

    public static int Count => Values.Length;
}