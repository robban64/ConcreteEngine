#region

using System.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Core.Rendering.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Core.Rendering.Batching;

internal sealed class SpriteBatchMesh : IDisposable
{
    private const int VerticesPerSprite = 4;
    private const int IndicesPerSprite = 6;

    //Static before
    private readonly Vertex2D[] Vertices = new Vertex2D[MaxSpriteBatchSize * VerticesPerSprite];
    private readonly ushort[] Indices = new ushort[MaxSpriteBatchSize * IndicesPerSprite];

    private readonly GfxContext _gfx;

    private readonly int _capacity;

    private readonly MeshId _meshId;
    private readonly VertexBufferId _vertexBufferId;
    private readonly IndexBufferId _indexBufferId;

    private bool _disposed = false;

    public SpriteBatchMesh(GfxContext gfx, int capacity)
    {
        _gfx = gfx;

        InitIndexBufferData();

        var indices = Indices.AsSpan(0, _capacity * IndicesPerSprite);


        var props = MeshDrawProperties.MakeTriElemental(size: DrawElementSize.UnsignedShort);
        var builder = gfx.Meshes.StartUploadBuilder(in props);
        builder.UploadVertices<Vertex2D>(Vertices, BufferUsage.StreamDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        builder.UploadIndices<ushort>(indices, BufferUsage.StreamDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var attribBuilder = new VertexAttributeMaker<Vertex2D>();
        builder.AddAttribute(attribBuilder.Make<Vector2>());
        builder.AddAttribute(attribBuilder.Make<Vector2>());

        var meshId = builder.Finish();
        var meshLayout = gfx.ResourceContext.Repository.MeshRepository.Get(meshId);
        _vertexBufferId = meshLayout.GetVertexBufferIds()[0];
        _indexBufferId = meshLayout.IndexBufferId;
    }

    private void InitIndexBufferData()
    {
        var indices = Indices.AsSpan(0, _capacity * IndicesPerSprite);
        for (int i = 0; i < _capacity; i++)
        {
            int vi = (ushort)(i * 4);
            int ii = (ushort)(i * 6);
            indices[ii + 0] = (ushort)(vi + 0);
            indices[ii + 1] = (ushort)(vi + 1);
            indices[ii + 2] = (ushort)(vi + 2);
            indices[ii + 3] = (ushort)(vi + 2);
            indices[ii + 4] = (ushort)(vi + 1);
            indices[ii + 5] = (ushort)(vi + 3);
        }
    }

    public SpriteBatchBuildResult BuildSpriteBatch(ReadOnlySpan<SpriteBatchDrawItem> commands)
    {
        int spriteCount = commands.Length;
        if (spriteCount == 0)
            return default;

        if (spriteCount > _capacity)
            throw new InvalidOperationException($"Sprite batch {spriteCount} exceeds maximum of {_capacity} sprites.");

        var vertices = Vertices.AsSpan(0, spriteCount * VerticesPerSprite);

        for (int i = 0; i < spriteCount; i++)
        {
            ref readonly var cmd = ref commands[i];

            var pos = cmd.Position;
            var size = cmd.Scale;

            var x = pos.X;
            var y = pos.Y;
            var w = size.X;
            var h = size.Y;

            var (u0, v0, u1, v1) = cmd.Uv;

            int vi = i * 4;

            //top left origin
            // Bottom-left
            vertices[vi + 0] = new Vertex2D(x, y, u0, v0);
            // Bottom-right
            vertices[vi + 1] = new Vertex2D(x + w, y, u1, v0);
            // Top-left
            vertices[vi + 2] = new Vertex2D(x, y + h, u0, v1);
            // Top-right
            vertices[vi + 3] = new Vertex2D(x + w, y + h, u1, v1);


            // Bottom-left (centered origin)
            /*

           var halfW = w * 0.5f;
           var halfH = h * 0.5f;

            vertices[vi + 0] = new Vertex2D(new(x - halfW, y - halfH), new(u0, v0));
            // Bottom-right
            vertices[vi + 1] = new Vertex2D(new(x + halfW, y - halfH), new(u1, v0));
            // Top-left
            vertices[vi + 2] = new Vertex2D(new(x - halfW, y + halfH), new(u0, v1));
            // Top-right
            vertices[vi + 3] = new Vertex2D(new(x + halfW, y + halfH), new(u1, v1));
            */
        }

        _gfx.Buffers.UploadVertexBuffer<Vertex2D>(_vertexBufferId, vertices, 0);

        var drawCount = (uint)(spriteCount * IndicesPerSprite);

        return new SpriteBatchBuildResult(_meshId, drawCount);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gfx.ResourceContext.Disposer.EnqueueRemoval(_meshId, false);
        _disposed = true;
    }
}