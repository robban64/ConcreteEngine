#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Rendering.Sprite;

internal sealed class SpriteBatch : IDisposable
{
    private const int VerticesPerSprite = 4;
    private const int IndicesPerSprite = 6;

    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;

    private readonly int _capacity;
    private readonly SpriteBatchDrawCommand _command;

    private readonly ushort _meshId;
    private readonly ushort _vertexBufferId;
    private readonly ushort _indexBufferId;

    private bool _disposed = false;

    public SpriteBatch(IGraphicsDevice graphics, int capacity)
    {
        var minSpriteBatchSize = graphics.Configuration.MinSpriteBatchSize;
        var maxSpriteBatchSize = graphics.Configuration.MaxSpriteBatchSize;

        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, minSpriteBatchSize, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, maxSpriteBatchSize, nameof(capacity));

        _graphics = graphics;
        _ctx = graphics.Ctx;
        _capacity = capacity;

        var meshData = new MeshDescriptor<Vertex2D>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.StreamDraw, null),
            IndexBuffer = new MeshDataBufferDescriptor<uint>(BufferUsage.StaticDraw, null),
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
        
        _ctx.BindVertexBuffer(meshResult.VertexBufferId);
        InitVertexBufferData();
        _ctx.BindVertexBuffer(0);

        _ctx.BindIndexBuffer(meshResult.IndexBufferId);
        InitIndexBufferData();
        _ctx.BindIndexBuffer(0);
        _command = new SpriteBatchDrawCommand { MeshId = _meshId };
    }

    private void InitVertexBufferData()
    {
        Span<Vertex2D> vertices = stackalloc Vertex2D[_capacity * VerticesPerSprite];
        _ctx.SetVertexBuffer<Vertex2D>(vertices);
    }

    private void InitIndexBufferData()
    {
        Span<uint> indices = stackalloc uint[_capacity * IndicesPerSprite];
        for (int i = 0; i < _capacity; i++)
        {
            int vi = i * 4;
            int ii = i * 6;
            indices[ii + 0] = (uint)(vi + 0);
            indices[ii + 1] = (uint)(vi + 1);
            indices[ii + 2] = (uint)(vi + 2);
            indices[ii + 3] = (uint)(vi + 2);
            indices[ii + 4] = (uint)(vi + 1);
            indices[ii + 5] = (uint)(vi + 3);
        }

        _ctx.SetIndexBuffer(indices);
    }

    public SpriteBatchDrawCommand BuildMesh(ReadOnlySpan<SpriteBatchDrawItem> commands)
    {
        int spriteCount = commands.Length;
        if (spriteCount == 0)
            return _command;

        if (spriteCount > _capacity)
            throw new InvalidOperationException($"Sprite batch {spriteCount} exceeds maximum of {_capacity} sprites.");

        Span<Vertex2D> vertices = stackalloc Vertex2D[spriteCount * VerticesPerSprite];

        for (int i = 0; i < spriteCount; i++)
        {
            ref readonly var cmd = ref commands[i];

            var pos = cmd.Position;
            var size = cmd.Scale;
            var uvOffset = cmd.TextureOffset;
            var uvScale = cmd.TextureScale;

            var x = pos.X;
            var y = pos.Y;
            var w = size.X;
            var h = size.Y;

            var halfW = w * 0.5f;
            var halfH = h * 0.5f;

            var u0 = uvOffset.X;
            var v0 = uvOffset.Y;
            var u1 = uvOffset.X + uvScale.X;
            var v1 = uvOffset.Y + uvScale.Y;

            int vi = i * 4;

            // Bottom-left (centered origin)
            vertices[vi + 0] = new Vertex2D(new(x - halfW, y - halfH), new(u0, v0));
            // Bottom-right
            vertices[vi + 1] = new Vertex2D(new(x + halfW, y - halfH), new(u1, v0));
            // Top-left
            vertices[vi + 2] = new Vertex2D(new(x - halfW, y + halfH), new(u0, v1));
            // Top-right
            vertices[vi + 3] = new Vertex2D(new(x + halfW, y + halfH), new(u1, v1));

            //top left origin
            /*
                // Bottom-left
               vertices[vi + 0] = new Vertex2D(new(x,     y    ), new(u0, v0));
               // Bottom-right
               vertices[vi + 1] = new Vertex2D(new(x + w, y    ), new(u1, v0));
               // Top-left
               vertices[vi + 2] = new Vertex2D(new(x,     y + h), new(u0, v1));
               // Top-right
               vertices[vi + 3] = new Vertex2D(new(x + w, y + h), new(u1, v1));
             */
        }

        _ctx.BindVertexBuffer(_vertexBufferId);
        _ctx.UploadVertexBuffer<Vertex2D>(vertices, 0);
        _ctx.BindVertexBuffer(0);

        _command.DrawCount = (uint)(spriteCount * IndicesPerSprite);

        _command.MeshId = _meshId;
        return _command;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _graphics.RemoveResource(_meshId);
        _disposed = true;
    }
}