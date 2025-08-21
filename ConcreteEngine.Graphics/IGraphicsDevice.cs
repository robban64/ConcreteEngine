#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Gfx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    MeshId QuadMeshId { get; }

    void StartFrame(in GraphicsFrameContext frameCtx);
    void EndFrame();
    
    UniformTable GetShaderUniforms(ShaderId shaderId);
    
    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, string[] samplers);
    TextureId CreateTexture2D(in TextureDesc textureDesc);
    VertexBufferId CreateVertexBuffer(BufferUsage bufferUsage);
    IndexBufferId CreateIndexBuffer(BufferUsage usage, IboElementType elementType);

    MeshId CreateMesh<TVertex, TIndex>(MeshDescriptor<TVertex, TIndex> meshData, out MeshMeta meta)
        where TVertex : unmanaged
        where TIndex : unmanaged;

    void EnqueueRemoveResource<TId>(TId id, bool replace = false) where TId : struct;
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Gfx { get; }
}