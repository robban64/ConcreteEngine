#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Error;

public sealed partial class GraphicsException(string message, Exception? inner = null)
    : Exception(message, inner)
{
    // Exceptions
    public static GraphicsException ResourceIsNull(string? name = null) => new($"{name} is null.");

    public static GraphicsException ResourceNotBound(string? name = null) =>
        new($"{name} is not bound to the pipeline.");

    public static GraphicsException ResourceIsDisposed(string? name = null) => new($"{name} is disposed.");
    public static GraphicsException ResourceIsDisposed(int id) => new($"{id} is disposed.");

    public static GraphicsException ResourceNotFound(object name) =>
        new($"{name} was not found.");

    public static GraphicsException ResourceNotFound(int id) => new($"{id} was not found.");

    public static GraphicsException ResourceAlreadyExists(object name) =>
        new($"{name} already exists.");

    public static GraphicsException ResourceAlreadyExists(int id) => new($"{id} already exists.");

    public static GraphicsException DuplicatedResource(object name) =>
        new($"Duplicated  in {name}");


    public static GraphicsException MissingHandle(string? name = null) =>
        new($"{name} has no valid GPU handle (was it created?).");

    public static GraphicsException InvalidBufferData(string? name, string reason) =>
        new($"{name} invalid buffer data: {reason}");


    public static GraphicsException InvalidState(string description) => new($"Invalid graphics state: {description}");

    public static GraphicsException InvalidType(string? name, object other) =>
        new($"Expected type: {name}. Actual type: {other.GetType().Name}");


    public static GraphicsException ShaderLinkFailed(string shaderName, string log) =>
        new($"Failed to link shader '{shaderName}'. Compiler log:\n{log}");

    public static GraphicsException ShaderCompileFailed(string shaderName, string log) =>
        new($"Failed to compile shader '{shaderName}'. Compiler log:\n{log}");


    public static GraphicsException FramebufferIncomplete(string fbName, string reason) =>
        new($"Framebuffer '{fbName}' is incomplete: {reason}");

    public static GraphicsException UnsupportedFeature(string feature) =>
        new($"The feature '{feature}' is not supported (yet).");

    public static GraphicsException LimitExceeded(string capabilityName, int limit) =>
        new($"{capabilityName} limit has been exceeded ({limit}).");

    public static GraphicsException CapabilityExceeded(string capabilityName, int attempted, int maximum) =>
        new($"{capabilityName} value {attempted} exceeds the maximum supported ({maximum}).");

    public static GraphicsException CapabilityTooLow(string capabilityName, int attempted, int minimum) =>
        new($"{capabilityName} value {attempted} is below the minimum required ({minimum}).");

    public static GraphicsException InvalidStd140Layout(int stride) =>
        new($"Ubo contains invalid std layout with stride: {stride}");

}