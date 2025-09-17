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
    void SetVertexAttribute(in GfxHandle vao, uint index, in VertexAttributeDescriptor attribute);
    
    void SetVertexBuffer<T>(in GfxHandle vao, in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;

    void UploadVertexBuffer<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offsetBytes) where T : unmanaged;
    
    void SetIndexBuffer<T>(in GfxHandle vao, in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged;
    void UploadIndexBuffer<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offsetBytes) where T : unmanaged;

    void SetUniformBufferSize(UniformGpuSlot slot, nuint capacity);
    void UploadUniformBuffer<T>(in GfxHandle ubo, in T data, nuint offset, nuint size) where T : unmanaged, IUniformGpuData;
    void BindUniformBufferRange(in GfxHandle ubo, UniformGpuSlot slot, nuint offset, nuint size);

}

internal interface IDrawBackend
{
    void DrawArrays(DrawPrimitive primitive, uint drawCount);
    void DrawElements(DrawPrimitive primitive, DrawElementType elementType, uint drawCount);
    void Blit(Vector2D<int> srcSize, Vector2D<int> dstSize, bool linear);
}


internal interface IBinderBackend
{
    void BindVertexArray(in GfxHandle vao);
    void BindVertexBuffer(in GfxHandle vbo);
    void BindIndexBuffer(in GfxHandle ibo);
    void BindFrameBuffer(in GfxHandle fbo);
    void BindRenderBuffer(in GfxHandle rbo);
    void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo);
    void BindUniformBuffer(in GfxHandle ubo);
    
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

internal interface IDeleteResourceBackend
{
    void DeleteGfxResource(in DeleteCmd cmd);
}

internal interface ICreateResourceBackend
{
    ResourceRefToken<ShaderId> CreateShader(string vs, string fs, out List<(string, int)> uniforms, out ShaderMeta meta);
    ResourceRefToken<TextureId> CreateEmptyTexture2D(in GpuTextureDescriptor desc, out TextureMeta meta);
    ResourceRefToken<TextureId> CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    ResourceRefToken<TextureId> CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);

    ResourceRefToken<MeshId> CreateVertexArray(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta);

    ResourceRefToken<VertexBufferId> CreateVertexBuffer(BufferUsage usage, uint elementSize, uint bindingIndex, out VertexBufferMeta meta);
    ResourceRefToken<IndexBufferId> CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta);

    ResourceRefToken<UniformBufferId> CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity capacity, uint blockSize,
        out UniformBufferMeta meta);
    
    ResourceRefToken<FrameBufferId> CreateFrameBuffer(out FrameBufferMeta meta);

    ResourceRefToken<RenderBufferId> CreateRenderBuffer(
        RenderBufferKind kind, Vector2D<int> size, bool multisample, uint samples, out RenderBufferMeta meta);
}

internal interface IGraphicsDriver : IGfxShaderBackend, IBufferBackend, IDrawBackend, IBinderBackend,
     IStateBackend, IDeleteResourceBackend, ICreateResourceBackend
{
    GraphicsConfiguration Configuration { get; }
    DeviceCapabilities Capabilities { get; }
    void AttachStore(BackendOpsHub storeHub);
    void AttachDispatcher(ResourceBackendDispatcher dispatcher);
    void ValidateEndFrame();
}