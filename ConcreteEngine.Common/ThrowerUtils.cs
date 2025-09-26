using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common;

public static class ArgumentExceptionThrower
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperation(string? param = null, string? message = null) =>
        throw new ArgumentOutOfRangeException($"Invalid operation: {param}");


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string? param = null, string? message = null)
    {
        if (condition) ThrowOperation(param, message);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNot(bool condition, string? param = null, string? message = null)
    {
        if (!condition) ThrowOperation(param, message);
    }
}

public static class InvalidOpThrower
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperation(string? param = null, string? message = null) =>
        throw new InvalidOperationException($"Invalid operation: {param}. {message}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(object? obj, string? param = null, string? message = null)
    {
        if (obj is null) ThrowOperation(param);
    }
    
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string? param = null, string? message = null)
    {
        if(condition) ThrowOperation(param);
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNot(bool condition, string? param = null, string? message = null)
    {
        if(!condition) ThrowOperation(param);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmptyCollection<T>(IReadOnlyCollection<T>? collection, string? paramName = null,
        string? message = null)
    {
        if (collection is null || collection.Count == 0) ThrowOperation(paramName, message);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCapacityExceed<T>(T[]? array, int capacity)
    {
        ArgumentNullException.ThrowIfNull(array, nameof(array));
        if (capacity > array.Length) 
            ThrowOperation(nameof(capacity), $"Capacity exceed {capacity} > {array.Length}");
    }

}