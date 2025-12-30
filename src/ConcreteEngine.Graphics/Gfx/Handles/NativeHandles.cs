using ConcreteEngine.Core.Specs.Graphics;

namespace ConcreteEngine.Graphics.Gfx.Handles;

internal readonly record struct NativeHandle(uint Value);

internal interface IResourceHandle
{
    uint Value { get; }
    static abstract GraphicsKind Kind { get; }

}

internal readonly record struct GlTextureHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlTextureHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.Texture;

}

internal readonly record struct GlShaderHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlShaderHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.Shader;

}

internal readonly record struct GlMeshHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlMeshHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.Mesh;

}

internal readonly record struct GlVboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlVboHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.VertexBuffer;

}

internal readonly record struct GlIboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlIboHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.IndexBuffer;

}

internal readonly record struct GlFboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlFboHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.FrameBuffer;

}

internal readonly record struct GlRboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlRboHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.RenderBuffer;

}

internal readonly record struct GlUboHandle(uint Value) : IResourceHandle
{
    public static implicit operator uint(GlUboHandle handle) => handle.Value;
    public static GraphicsKind Kind => GraphicsKind.UniformBuffer;

}