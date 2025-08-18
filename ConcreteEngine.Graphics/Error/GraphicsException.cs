namespace ConcreteEngine.Graphics.Error;

public sealed partial class GraphicsException : InvalidOperationException
{
    public GraphicsException(string message) : base(message)
    {
    }


    // Exceptions
    public static GraphicsException ResourceIsNull<T>(string? name = null) =>
        new($"{Label<T>(name)} is null.");

    public static GraphicsException ResourceNotBound<T>(string? name = null) =>
        new($"{Label<T>(name)} is not bound to the pipeline.");

    public static GraphicsException ResourceIsDisposed<T>(string? name = null) => new($"{Label<T>(name)} is disposed.");
    public static GraphicsException ResourceIsDisposed(int id) => new($"{id} is disposed.");

    public static GraphicsException ResourceNotFound<T>(object name) =>
        new($"{Label<T>(name.ToString())} was not found.");

    public static GraphicsException ResourceNotFound(int id) => new($"{id} was not found.");


    public static GraphicsException ResourceAlreadyExists<T>(object name) =>
        new($"{Label<T>(name.ToString())} already exists.");

    public static GraphicsException ResourceAlreadyExists(int id) => new($"{id} already exists.");


    public static GraphicsException MissingHandle<T>(string? name = null) =>
        new($"{Label<T>(name)} has no valid GPU handle (was it created?).");

    public static GraphicsException InvalidBufferData<T>(string? name, string reason) =>
        new($"{Label<T>(name)} invalid buffer data: {reason}");


    public static GraphicsException InvalidState(string description) =>
        new($"Invalid graphics state: {description}");

    public static GraphicsException InvalidType<T>(string? name, object other) =>
        new($"Expected type: {Label<T>(name)}. Actual type: {other.GetType().Name}");

    public static GraphicsException ShaderLinkFailed(string shaderName, string log) =>
        new($"Failed to link shader '{shaderName}'. Compiler log:\n{log}");

    public static GraphicsException ShaderCompileFailed(string shaderName, string log) =>
        new($"Failed to compile shader '{shaderName}'. Compiler log:\n{log}");


    public static GraphicsException FramebufferIncomplete(string fbName, string reason) =>
        new($"Framebuffer '{fbName}' is incomplete: {reason}");

    public static GraphicsException UnsupportedFeature(string feature) =>
        new($"The feature '{feature}' is not supported by this hardware or context.");

    public static GraphicsException CapabilityExceeded<T>(string capabilityName, int attempted, int maximum) =>
        new($"{Label<T>()}: {capabilityName} value {attempted} exceeds the maximum supported ({maximum}).");

    public static GraphicsException CapabilityTooLow<T>(string capabilityName, int attempted, int minimum) =>
        new($"{Label<T>()}: {capabilityName} value {attempted} is below the minimum required ({minimum}).");


    private static string Label<T>(string? name = null)
    {
        var s = typeof(T).Name;
        return string.IsNullOrWhiteSpace(name) ? s : $"{s} '{name}'";
    }
}