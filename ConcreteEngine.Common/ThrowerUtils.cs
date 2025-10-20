#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using static System.Enum;

#endregion

namespace ConcreteEngine.Common;

[StackTraceHidden]
public static class ArgOutOfRangeThrower
{
    public static void ThrowIfSizeTooSmall(Size2D size, int min)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Width, min, nameof(size));
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Height, min, nameof(size));
    }

    public static void ThrowIfSizeTooBig(Size2D size, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size.Width, max, nameof(size));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size.Height, max, nameof(size));
    }

    public static void ThrowIfSizeTooSmall(Size2D size, Size2D min)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Width, min.Width, nameof(size));
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Height, min.Height, nameof(size));
    }

    public static void ThrowIfSizeTooBig(Size2D size, Size2D max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size.Width, max.Width, nameof(size));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size.Height, max.Height, nameof(size));
    }
}

[StackTraceHidden]
public static class InvalidOpThrower
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperation(string? param = null, string? message = null) =>
        throw new InvalidOperationException($"Invalid operation: {param}. {message}");

    public static void ThrowIfSizeTooSmall(Size2D size, int min, string? message = null)
    {
        if (size.Width < min || size.Height < min)
            ThrowOperation(message ?? $"Size too small: ({size.Width}x{size.Height}) < {min}");
    }

    public static void ThrowIfSizeTooBig(Size2D size, int max, string? message = null)
    {
        if (size.Width > max || size.Height > max)
            ThrowOperation(message ?? $"Size too big: ({size.Width}x{size.Height}) > {max}");
    }

    public static void ThrowIfSizeTooSmall(Size2D size, Size2D min, string? message = null)
    {
        if (size.Width < min.Width || size.Height < min.Height)
            ThrowOperation(message ?? $"Size too small: ({size.Width}x{size.Height}) < ({min.Width}x{min.Height})");
    }

    public static void ThrowIfSizeTooBig(Size2D size, Size2D max, string? message = null)
    {
        if (size.Width > max.Width || size.Height > max.Height)
            ThrowOperation(message ?? $"Size too big: ({size.Width}x{size.Height}) > ({max.Width}x{max.Height})");
    }

    
    public static void ThrowIfNull(object? obj, string? param = null, string? message = null)
    {
        if (obj is null) ThrowOperation(param, message);
    }

    public static void ThrowIfAnyNull(object? o1, object? o2, string? param = null, string? message = null)
    {
        if (o1 is null || o2 is null) ThrowOperation(param, message);
    }

    public static void ThrowIfAnyNull(object? o1, object? o2, object? o3, string? param = null, string? message = null)
    {
        if (o1 is null || o2 is null || o3 is null) ThrowOperation(param, message);
    }

    public static void ThrowIfAnyNull(object? o1, object? o2, object? o3, object? o4, string? param = null,
        string? message = null)
    {
        if (o1 is null || o2 is null || o3 is null || o4 is null) ThrowOperation(param, message);
    }


    public static void ThrowIfNotNull(object? obj, string? param = null, string? message = null)
    {
        if (obj is not null) ThrowOperation(param, message);
    }

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

    public static void ThrowIfNullOrEmptyCollection<T>(IReadOnlyCollection<T>? collection, string? paramName = null,
        string? message = null)
    {
        if (collection is null || collection.Count == 0) ThrowOperation(paramName, message);
    }

    public static void ThrowIfCapacityExceed<T>(T[]? array, int capacity)
    {
        ArgumentNullException.ThrowIfNull(array, nameof(array));
        if (capacity > array.Length)
            ThrowOperation(nameof(capacity), $"Capacity exceed {capacity} > {array.Length}");
    }
}