#region

using System.Diagnostics.CodeAnalysis;

#endregion

namespace ConcreteEngine.Graphics.Error;

public sealed partial class GraphicsException
{
    [DoesNotReturn]
    public static void ThrowResourceIsNull<T>(string? name = null) =>
        throw ResourceIsNull<T>(name);


    [DoesNotReturn]
    public static void ThrowResourceNotBound<T>(string? name = null) =>
        throw ResourceNotBound<T>(name);

    [DoesNotReturn]
    public static void ThrowResourceIsDisposed<T>(string? name = null) =>
        throw ResourceIsDisposed<T>(name);

    public static void ThrowResourceIsDisposed(int id) =>
        throw ResourceIsDisposed(id);


    [DoesNotReturn]
    public static void ThrowResourceNotFound<T>(object name) =>
        throw ResourceNotFound<T>(name);

    [DoesNotReturn]
    public static void ThrowResourceNotFound(int id) =>
        throw ResourceNotFound(id);


    [DoesNotReturn]
    public static void ThrowResourceAlreadyExists<T>(object name) =>
        throw ResourceAlreadyExists<T>(name);

    [DoesNotReturn]
    public static void ThrowResourceAlreadyExists(int id) =>
        throw ResourceAlreadyExists(id);


    [DoesNotReturn]
    public static void ThrowMissingHandle<T>(string? name = null) =>
        throw MissingHandle<T>(name);

    [DoesNotReturn]
    public static void ThrowInvalidBufferData<T>(string? name, string reason) =>
        throw InvalidBufferData<T>(name, reason);

    [DoesNotReturn]
    public static void ThrowInvalidState(string description) =>
        throw InvalidState(description);

    [DoesNotReturn]
    public static void ThrowInvalidType<T>(string? name, object other) =>
        throw InvalidType<T>(name, other);

    [DoesNotReturn]
    public static void ThrowShaderLinkFailed(string shaderName, string log) =>
        throw ShaderLinkFailed(shaderName, log);

    [DoesNotReturn]
    public static void ThrowShaderCompileFailed(string shaderName, string log) =>
        throw ShaderCompileFailed(shaderName, log);

    [DoesNotReturn]
    public static void ThrowFramebufferIncomplete(string fbName, string reason) =>
        throw FramebufferIncomplete(fbName, reason);

    [DoesNotReturn]
    public static void ThrowUnsupportedFeature(string feature) =>
        throw UnsupportedFeature(feature);

    [DoesNotReturn]
    public static void ThrowCapabilityExceeded<T>(string capabilityName, int attempted, int maximum) =>
        throw CapabilityExceeded<T>(capabilityName, attempted, maximum);

    [DoesNotReturn]
    public static void ThrowCapabilityTooLow<T>(string capabilityName, int attempted, int minimum) =>
        throw CapabilityTooLow<T>(capabilityName, attempted, minimum);
}