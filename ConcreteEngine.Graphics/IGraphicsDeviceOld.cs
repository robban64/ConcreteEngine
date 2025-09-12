#region

using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Graphics;


public interface IGraphicsDeviceOld : IDisposable
{
    IGraphicsContext Gfx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    IPrimitiveMeshes Primitives { get; }
    IShaderRegistry ShaderRegistry { get; }
    IMeshRegistry MeshRegistry { get; }
    IMeshFactory MeshFactory { get; }
    GpuResourceBuilder CreateBuilder();
    IGpuUploadSink CreateUploader();
    
    void BuildResources(GpuResourceBuilder builder);
    void StartFrame(in FrameInfo frameCtx);
    void EndFrame(out GpuFrameStats result);

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);
    TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);
    MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement);
    VertexBufferId CreateVertexBuffer( BufferUsage bufferUsage, uint elementSize, uint bindingIndex = 0);
    IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize);

    UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity, out UniformBufferMeta meta)
        where T :  unmanaged, IUniformGpuData;

    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : unmanaged, IResourceId;
}

//IGraphicsDevice should not be graphics api implemented, remove
public interface IGraphicsDeviceOld<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Gfx { get; }
}