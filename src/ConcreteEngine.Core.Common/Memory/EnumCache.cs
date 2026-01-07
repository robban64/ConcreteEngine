using System;

namespace ConcreteEngine.Core.Common.Memory;

public static class EnumCache<T> where T : unmanaged, Enum
{
    private static readonly T[] Values = Enum.GetValues<T>();
    private static readonly string[] Names = Enum.GetNames<T>();

    public static ReadOnlySpan<T> ValueSpan => Values;
    public static ReadOnlySpan<string> NameSpan => Names;

    public static int Count => Values.Length;
}