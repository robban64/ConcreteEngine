using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics;

//TODO

public enum GraphicsResourceType: ushort{
    Texture=0,
    Shader=1,
    Mesh=2,
    Buffer=3,
    RenderTarget=4
}

public readonly struct GlTextureHandle(uint handle, int width, int height, PixelFormat format)
{
    public readonly uint Handle = handle;
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly PixelFormat Format = format;
}

public readonly struct GlShaderHandle(uint handle, uint samplers)
{
    public readonly uint Handle = handle;
    public readonly uint Samplers = samplers;
}

public readonly struct GlMeshHandle(
    uint handle,
    ushort vertexBufferId,
    ushort indexBufferId,
    uint drawCount,
    bool isStaticMesh)
{
    public readonly uint Handle = handle;
    public readonly ushort VertexBufferId = vertexBufferId;
    public readonly ushort IndexBufferId = indexBufferId;
    public readonly uint DrawCount = drawCount;
    public readonly bool IsStaticMesh = isStaticMesh;
}

public readonly struct GlBufferHandle(
    uint handle,
    BufferUsageARB usage,
    BufferTargetARB target,
    uint elementCount,
    uint elementSize
)
{
    public readonly uint Handle = handle;
    public readonly BufferUsageARB Usage = usage;
    public readonly BufferTargetARB Target = target;
    public readonly uint ElementCount = elementCount;
    public readonly uint ElementSize = elementSize;
}

public readonly struct GlRenderTargetHandle(uint handle, uint sample)
{
    public readonly uint Handle = handle;
    public readonly uint Sample = sample;
}