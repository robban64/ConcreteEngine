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
    IShader CreateShader(string vertexSource, string fragmentSource);
    ITexture2D CreateTexture2D(in TextureDescriptor textureDescriptor);
    IGraphicsBuffer CreateBuffer(BufferTarget target, BufferUsage usage);
    IMesh CreateMesh<TBuffer>(MeshDescriptor<TBuffer> meshData) where TBuffer : unmanaged;
    void RemoveResource<TResource>(TResource resource) where TResource : IGraphicsResource;
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Ctx { get; }
}