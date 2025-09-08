#region

using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Gfx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    IPrimitiveMeshes Primitives { get; }

    GraphicsResourceBuilder CreateBuilder();
    void LoadResources(GpuResourcePayloadCollection payloadCollection);
    void BuildResources(GraphicsResourceBuilder builder);
    bool ProcessResources();
    void StartFrame(in FrameMetaInfo frameCtx);
    void EndFrame(out FrameRenderResult result);

    UniformBufferId GetUboIdBySlot(UniformGpuSlot slot);

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);
    TextureId CreateTexture2D(GpuTexturePayload payload, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapPayload cubemapDesc, out TextureMeta meta);

    VertexBufferId CreateVertexBuffer(BufferUsage bufferUsage);
    IndexBufferId CreateIndexBuffer(BufferUsage usage, IboElementType elementType);

    MeshId CreateMesh<TVertex, TIndex>(in GpuMeshData<TVertex, TIndex> dataDesc, in GpuMeshDescriptor desc,
        out MeshMeta meta) where TVertex : unmanaged where TIndex : unmanaged;


    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : struct;
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Gfx { get; }
}