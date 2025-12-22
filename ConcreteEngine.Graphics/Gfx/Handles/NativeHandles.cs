namespace ConcreteEngine.Graphics.Gfx.Resources.Handles;

internal readonly record struct NativeHandle(uint Value);

internal interface IResourceHandle
{
    uint Value { get; }
}

internal readonly record struct GlTextureHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlTextureHandle handle) => handle.Value;
}

internal readonly record struct GlShaderHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlShaderHandle handle) => handle.Value;
}

internal readonly record struct GlMeshHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlMeshHandle handle) => handle.Value;
}

internal readonly record struct GlVboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlVboHandle handle) => handle.Value;
}

internal readonly record struct GlIboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlIboHandle handle) => handle.Value;
}

internal readonly record struct GlFboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlFboHandle handle) => handle.Value;
}

internal readonly record struct GlRboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlRboHandle handle) => handle.Value;
}

internal readonly record struct GlUboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlUboHandle handle) => handle.Value;
}