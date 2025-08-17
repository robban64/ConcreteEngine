#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;

public readonly struct SpriteBatchBuildResult(ushort meshId, uint drawCount)
{
    public readonly ushort MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}

internal sealed class SpriteBatchMesh : IDisposable
{
    private const int VerticesPerSprite = 4;
    private const int IndicesPerSprite = 6;
    
    private static readonly Vertex2D[] Vertices = new Vertex2D[MaxSpriteBatchSize * VerticesPerSprite];
    private static readonly ushort[] Indices = new ushort[MaxSpriteBatchSize * IndicesPerSprite];

    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;

    private readonly int _capacity;


    private readonly ushort _meshId;
    private readonly ushort _vertexBufferId;
    private readonly ushort _indexBufferId;

    private bool _disposed = false;
    
    public SpriteBatchMesh(IGraphicsDevice graphics, int capacity)
    {
        var minSpriteBatchSize = graphics.Configuration.MinSpriteBatchSize;
        var maxSpriteBatchSize = graphics.Configuration.MaxSpriteBatchSize;

        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, minSpriteBatchSize, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, maxSpriteBatchSize, nameof(capacity));

        _graphics = graphics;
        _gfx = graphics.Gfx;
        _capacity = capacity;

        var meshData = new MeshDescriptor<Vertex2D, ushort>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StreamDraw, null),
            IndexBuffer = new MeshDataBufferDescriptor<ushort>(BufferUsage.StaticDraw, null),
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex2D>("aPos", nameof(Vertex2D.Position)),
                VertexAttributeDescriptor.Make<Vertex2D>("aTex", nameof(Vertex2D.Texture))
            ]
        };

        var meshResult = _graphics.CreateMesh(meshData);
        _meshId = meshResult.MeshId;
        _vertexBufferId = meshResult.VertexBufferId;
        _indexBufferId = meshResult.IndexBufferId;

        _gfx.BindVertexBuffer(meshResult.VertexBufferId);
        InitVertexBufferData();
        _gfx.BindVertexBuffer(0);

        _gfx.BindIndexBuffer(meshResult.IndexBufferId);
        InitIndexBufferData();
        _gfx.BindIndexBuffer(0);
    }

    private void InitVertexBufferData()
    {
        _gfx.SetVertexBuffer<Vertex2D>(Vertices);
    }

    private void InitIndexBufferData()
    {
        var indices = Indices.AsSpan(0,_capacity * IndicesPerSprite);
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


        _gfx.SetIndexBuffer<ushort>(indices);
    }

    public SpriteBatchBuildResult BuildSpriteBatch(ReadOnlySpan<SpriteDrawData> commands)
    {
        int spriteCount = commands.Length;
        if (spriteCount == 0)
            return default;

        if (spriteCount > _capacity)
            throw new InvalidOperationException($"Sprite batch {spriteCount} exceeds maximum of {_capacity} sprites.");

        var vertices = Vertices.AsSpan(0,spriteCount * VerticesPerSprite);

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

        _gfx.BindVertexBuffer(_vertexBufferId);
        _gfx.UploadVertexBuffer<Vertex2D>(vertices, 0);
        _gfx.BindVertexBuffer(0);


        var drawCount = (uint)(spriteCount * IndicesPerSprite);

        return new SpriteBatchBuildResult(_meshId, drawCount);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _graphics.RemoveResource(_meshId);
        _disposed = true;
    }
}