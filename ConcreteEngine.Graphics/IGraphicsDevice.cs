#region

using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Graphics;


public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Gfx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    IPrimitiveMeshes Primitives { get; }
    IShaderRegistry ShaderRegistry { get; }
    GpuResourceBuilder CreateBuilder();
    IGpuUploadSink CreateUploader();
    
    void BuildResources(GpuResourceBuilder builder);
    void StartFrame(in FrameInfo frameCtx);
    void EndFrame(out GpuFrameStats result);

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);
    TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);
    MeshId CreateMesh<TVertex, TIndex>(in GpuMeshData<TVertex, TIndex> data, in GpuMeshDescriptor desc,
        out MeshMeta meta) where TVertex : unmanaged where TIndex : unmanaged;

    VertexBufferId CreateVertexBuffer(BufferUsage bufferUsage);
    IndexBufferId CreateIndexBuffer(BufferUsage usage, IboElementType elementType);

    UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity, out UniformBufferMeta meta)
        where T :  unmanaged, IUniformGpuData;

    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : struct;
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Gfx { get; }
}