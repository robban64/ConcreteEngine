using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common;

public static class InvalidOpThrower
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowOperation(string? param = null, string? message = null)
        => throw new InvalidOperationException(message ?? (param is null ? "Invalid operation." : $"Invalid operation: {param}"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue(bool condition, string? param = null, string? message = null)
    {
        if(condition) ThrowOperation(param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse(bool condition, string? param = null, string? message = null)
    {
        if(!condition) ThrowOperation(param);
    }

}

public static class StructThrower
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowNull<T>(string? paramName) where T : struct => throw new ArgumentNullException(paramName);
    
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowOperation<T>(string? paramName) where T : struct => throw new InvalidOperationException(paramName);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfNullStruct<T>(T? value, string? paramName = null) where T : struct
        => value ?? ThrowNull<T>(paramName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotNullStruct<T>(T? value, string? paramName = null) where T : struct
    {
        if(!value.HasValue) ThrowOperation<T>(paramName);
    }

}