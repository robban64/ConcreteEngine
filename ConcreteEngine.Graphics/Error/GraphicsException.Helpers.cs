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
    public static void ThrowInvalidAction(string paramName) =>
        throw new GraphicsException($"Invalid action: {paramName}");

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotBound(string? name = null) => throw ResourceNotBound(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceIsDisposed(string? name = null) => throw ResourceIsDisposed(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceIsDisposed(int id) => throw ResourceIsDisposed(id);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotFound(object name) => throw ResourceNotFound(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowResourceNotFound(int id) => throw ResourceNotFound(id);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowMissingHandle(string? name = null) => throw MissingHandle(name);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidBufferData(string? name, string reason) => throw InvalidBufferData(name, reason);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidState(string description) => throw InvalidState(description);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidType(string? name, object other) => throw InvalidType(name, other);

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
    public static void ThrowCapabilityExceeded(string capabilityName, int attempted, int maximum) =>
        throw CapabilityExceeded(capabilityName, attempted, maximum);

    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowCapabilityTooLow(string capabilityName, int attempted, int minimum) =>
        throw CapabilityTooLow(capabilityName, attempted, minimum);
}