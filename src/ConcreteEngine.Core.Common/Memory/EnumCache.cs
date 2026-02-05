namespace ConcreteEngine.Core.Common.Memory;

public static class EnumCache<T> where T : unmanaged, Enum
{
    public static readonly T[] Values = Enum.GetValues<T>();
    public static readonly string[] Names = Enum.GetNames<T>();

    public static int Count => Values.Length;
}