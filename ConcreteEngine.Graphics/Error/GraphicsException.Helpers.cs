#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Error;

public sealed partial class GraphicsException
{
    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowInvalidAction<T>(string paramName) =>
        throw new GraphicsException($"Invalid action: {paramName} - {typeof(T).Name}.");

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotBound<T>(string? name = null) => throw ResourceNotBound<T>(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceIsDisposed<T>(string? name = null) => throw ResourceIsDisposed<T>(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceIsDisposed(int id) => throw ResourceIsDisposed(id);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotFound<T>(object name) => throw ResourceNotFound<T>(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotFound(int id) => throw ResourceNotFound(id);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowMissingHandle<T>(string? name = null) => throw MissingHandle<T>(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidBufferData<T>(string? name, string reason) =>
        throw InvalidBufferData<T>(name, reason);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidState(string description) => throw InvalidState(description);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidType<T>(string? name, object other) => throw InvalidType<T>(name, other);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowShaderLinkFailed(string shaderName, string log) => throw ShaderLinkFailed(shaderName, log);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowShaderCompileFailed(string shaderName, string log) =>
        throw ShaderCompileFailed(shaderName, log);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowFramebufferIncomplete(string fbName, string reason) =>
        throw FramebufferIncomplete(fbName, reason);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUnsupportedFeature(string feature) => throw UnsupportedFeature(feature);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowCapabilityExceeded<T>(string capabilityName, int attempted, int maximum) =>
        throw CapabilityExceeded<T>(capabilityName, attempted, maximum);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowCapabilityTooLow<T>(string capabilityName, int attempted, int minimum) =>
        throw CapabilityTooLow<T>(capabilityName, attempted, minimum);
}