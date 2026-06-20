using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common;

[StackTraceHidden]
public static class Throwers
{
    // Arguments
    [DoesNotReturn]
    public static void InvalidArgument(string paramName, string? message = null) =>
        throw new ArgumentException(message, paramName);

    [DoesNotReturn]
    public static void IndexOutOfRange(string buffer, int index, int length) =>
        throw new ArgumentOutOfRangeException(buffer, $"OutOfRange {index} with length {length}");

    [DoesNotReturn]
    public static void NullReference(string name) => throw new ArgumentNullException(name, "Null arg reference");

    [DoesNotReturn]
    public static void NotFoundBy<T>(string name, T handle) => throw new ArgumentException(handle?.ToString(), name);

    [DoesNotReturn]
    public static void NotFound(string name, string message) =>
        throw new ArgumentException($"{message} not found", name);

    [DoesNotReturn]
    public static T Unreachable<T>(string name) => throw new UnreachableException(name);

    [DoesNotReturn]
    public static void Unreachable(string name) => throw new UnreachableException(name);

    [DoesNotReturn]
    public static void InvalidOperation(string? message = null) => throw new InvalidOperationException(message);

    [DoesNotReturn]
    public static void NullPointer(string name) => throw new InvalidOperationException($"Null pointer: {name}");

    [DoesNotReturn]
    public static void KeyNotFound<T>(T key) =>
        throw new InvalidOperationException($"{key} not found or incorrect type.");

    [DoesNotReturn]
    public static void InvalidHandle<T>(T handle) =>
        throw new InvalidOperationException($"Invalid handle ({typeof(T).Name}) = {handle}");

    [DoesNotReturn]
    public static void BufferOverflow(string message) => throw new InsufficientMemoryException(message);

    [DoesNotReturn]
    public static void BufferOverflow(string buffer, int size, int limit) =>
        throw new InsufficientMemoryException($"{buffer} with size {size} exceeded max limit:  {limit}");
}

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
    private static void ThrowOperation(string? param = null, string? message = null) =>
        throw new InvalidOperationException($"Invalid operation: {param}. {message}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string? param = null, string? message = null)
    {
        if (condition) ThrowOperation(param, message);
    }
}