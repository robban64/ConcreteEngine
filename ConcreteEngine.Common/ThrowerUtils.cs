using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common;

public static class ArgumentExceptionThrower
{
    
}
public static class InvalidOpThrower
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOperation(string? param = null, string? message = null)
        => throw new InvalidOperationException(message ?? (param is null ? "Invalid operation." : $"Invalid operation: {param}"));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(object? obj, string? param = null, string? message = null) 
    {
        if(obj is null) ThrowOperation(param, message);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty<T>(ICollection<T>? obj, string? param = null, string? message = null) 
    {
        if(obj is null) ThrowOperation(param, message);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string? param = null, string? message = null)
    {
        if(condition) ThrowOperation(param, message);
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNot(bool condition, string? param = null, string? message = null)
    {
        if(!condition) ThrowOperation(param, message);
    }

}