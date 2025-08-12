#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Rendering;
using ConcreteEngine.Graphics.Rendering.Sprite;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Ctx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    RenderPipeline RenderPipeline { get; }
    SpriteBatchController SpriteBatchController { get; }
    void StartFrame(in RenderFrameContext frameCtx);
    void EndFrame();
    int CreateShader(string vertexSource, string fragmentSource);
    int CreateTexture2D(in TextureDescriptor textureDescriptor);
    int CreateBuffer(BufferTarget target, BufferUsage usage);
    int CreateVertexBuffer(BufferUsage bufferUsage);
    int CreateIndexBuffer(BufferUsage bufferUsage);
    CreateMeshResult CreateMesh<TBuffer>(MeshDescriptor<TBuffer> meshData) where TBuffer : unmanaged;
    void RemoveResource(int resourceId);
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Ctx { get; }
}