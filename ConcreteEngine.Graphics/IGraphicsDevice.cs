#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsDevice : IDisposable
{
    IGraphicsContext Ctx { get; }
    GraphicsBackend BackendApi { get; }
    GraphicsConfiguration Configuration { get; }
    void CleanupAfterRender();
    ushort CreateShader(string vertexSource, string fragmentSource, string[] samplers);
    ushort CreateTexture2D(in TextureDescriptor textureDescriptor);
    ushort CreateBuffer(BufferTarget target, BufferUsage usage);
    ushort CreateVertexBuffer(BufferUsage bufferUsage);
    ushort CreateIndexBuffer(BufferUsage bufferUsage);
    RenderPassDesc CreateFrameBuffer(in CreateRenderPassDesc desc);

    CreateMeshResult CreateMesh<TVertex, TIndex>(MeshDescriptor<TVertex, TIndex> meshData)
        where TVertex : unmanaged
        where TIndex : unmanaged;

    void RemoveResource(ushort resourceId);
}

public interface IGraphicsDevice<out TContext> : IGraphicsDevice where TContext : class, IGraphicsContext
{
    new TContext Ctx { get; }
}