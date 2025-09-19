using ConcreteEngine.Common;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

internal interface IBufferBackend
{
    void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute);

    void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nint offsetBytes) where T : unmanaged;

    void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nint offsetBytes) where T : unmanaged;

    void SetUniformBufferSize(UniformGpuSlot slot, nint capacity);

    void UploadUniformBuffer<T>(in GfxHandle ubo, in T data, nint offset, nint size)
        where T : unmanaged, IUniformGpuData;

    void BindUniformBufferRange(in GfxHandle ubo, UniformGpuSlot slot, nint offset, nint size);
}

internal interface IDrawBackend
{
    void DrawArrays(DrawPrimitive primitive, uint drawCount);
    void DrawElements(DrawPrimitive primitive, DrawElementSize elementSize, uint drawCount);
    void Blit(Vector2D<int> srcSize, Vector2D<int> dstSize, bool linear);
}


internal interface IStateBackend
{
    void PrepareFrame();
    void Clear(Color4 color, ClearBufferFlag flags);
    void SetBlendMode(BlendMode blendMode);
    void SetDepthMode(DepthMode depthMode);
    void SetCullMode(CullMode cullMode);
    void SetViewport(in Vector2D<int> viewport);
    void BindTextureUnit(in GfxHandle tex, uint slot);
    void UseShader(in GfxHandle shader);
}

internal interface IDisposerBackend
{
    void DeleteGfxResource(in DeleteCmd cmd);
}

internal interface IGraphicsDriverModule;

internal interface IGraphicsDriver
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }
    void ValidateEndFrame();

    GlDebugger Debugger { get; }
    GlDisposer Disposer { get; }
    GlBuffers Buffers { get; }
    GlTextures Textures { get; }
    GlMeshes Meshes { get; }
    GlShaders Shaders { get; }
    GlStates States { get; }
    GlFrameBuffers FrameBuffers { get; }
}