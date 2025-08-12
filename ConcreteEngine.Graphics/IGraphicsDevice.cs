#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Ctx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    void StartFrame(in RenderFrameContext frameCtx);
    void StartDraw();
    void EndFrame();
    ushort CreateShader(string vertexSource, string fragmentSource);
    ushort CreateTexture2D(in TextureDescriptor textureDescriptor);
    ushort CreateBuffer(BufferTarget target, BufferUsage usage);
    ushort CreateVertexBuffer(BufferUsage bufferUsage);
    ushort CreateIndexBuffer(BufferUsage bufferUsage);
    CreateMeshResult CreateMesh<TBuffer>(MeshDescriptor<TBuffer> meshData) where TBuffer : unmanaged;
    void RemoveResource(ushort resourceId);
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Ctx { get; }
}