using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

internal interface IGfxShaderBackend
{
    GfxHandle CreateShader(string vs, string fs, out ShaderLayout layout, out ShaderMeta meta);
    void UseShader(in GfxHandle shader);
    void SetUniform(int uniform, int value);
    void SetUniform(int uniform, uint value);
    void SetUniform(int uniform, float value);
    void SetUniform(int uniform, Vector2 value);
    void SetUniform(int uniform, Vector3 value);
    void SetUniform(int uniform, Vector4 value);
    void SetUniform(int uniform, in Matrix4x4 value);
    void SetUniform(int uniform, in Matrix3 value);
}

internal interface IBufferBackend
{
    GfxHandle CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity capacity, uint blockSize,
        out UniformBufferMeta meta);

    void BindUniformBuffer(in GfxHandle ubo);
    void SetUniformBufferSize(UniformGpuSlot slot, nuint capacity);
    void UploadUniformBuffer<T>(in GfxHandle ubo, in T data, nuint offset, nuint size) where T : unmanaged, IUniformGpuData;
    void BindUniformBufferRange(in GfxHandle ubo, UniformGpuSlot slot, nuint offset, nuint size);
}

internal interface IMeshBackend
{
    GfxHandle CreateVertexArray(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta);

    GfxHandle CreateVertexBuffer(BufferUsage usage, uint elementSize, uint bindingIndex, out VertexBufferMeta meta);
    GfxHandle CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta);

    void BindVertexArray(in GfxHandle vao);
    void BindVertexBuffer(in GfxHandle vbo);
    void BindIndexBuffer(in GfxHandle ibo);
    void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute);

    void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offsetBytes) where T : unmanaged;
    void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offsetBytes) where T : unmanaged;

    void DrawArrays(DrawPrimitive primitive, uint drawCount);
    void DrawElements(DrawPrimitive primitive, DrawElementType elementType, uint drawCount);
}

internal interface ITextureBackend
{
    GfxHandle CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    GfxHandle CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);
    void BindTextureUnit(in GfxHandle tex, uint slot);
}

internal interface IFramebufferBackend
{
    void BindFramebuffer(in GfxHandle fbo);
    void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo);
    void CreateFramebuffer( in FrameBufferDesc desc, out DriverCreateFboResult result);
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
}

internal interface IDisposableBackend
{
    void DeleteGfxResource(GfxHandle handle, bool replace);
}

internal interface IGraphicsDriver : IGfxShaderBackend, IBufferBackend, IMeshBackend, ITextureBackend,
    IFramebufferBackend, IStateBackend, IDisposableBackend
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }
    void ValidateEndFrame();
}