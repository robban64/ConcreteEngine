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

    void InitializeData();

    void StartFrame(in FrameMetaInfo frameCtx);
    void EndFrame(out FrameRenderResult result);

    UniformTable GetShaderUniforms(ShaderId shaderId);
    IReadOnlyList<UniformBufferId> GetUniformBuffersBySlot(ShaderBufferUniform slot);
    UniformBufferId GetUboIdBySlot(ShaderBufferUniform slot);

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, string[] samplers);
    TextureId CreateTexture2D(in TextureDesc textureDesc);
    TextureId CreateCubeMap(in CreateCubemapDesc cubemapDesc);

    VertexBufferId CreateVertexBuffer(BufferUsage bufferUsage);
    IndexBufferId CreateIndexBuffer(BufferUsage usage, IboElementType elementType);

    MeshId CreateMesh<TVertex, TIndex>(in MeshDataDescriptor<TVertex, TIndex> dataDesc, in MeshMetaDescriptor metaDesc,
        out MeshMeta meta) where TVertex : unmanaged where TIndex : unmanaged;


    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : struct;
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Gfx { get; }
}