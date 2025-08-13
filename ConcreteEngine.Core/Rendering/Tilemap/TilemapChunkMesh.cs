using System.Numerics;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering.Tilemap;

internal sealed class TilemapChunkMesh : IDisposable
{
    private const int VerticesPerTile = 4;
    private const int IndicesPerTile = 6;
    private const int ChunkSize = 64;

    private static readonly Vertex2D[] Vertices = 
        new Vertex2D[ChunkSize * ChunkSize * VerticesPerTile];
    
    private static readonly ushort[] Indices = 
        new ushort[ChunkSize * ChunkSize * IndicesPerTile];

    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _ctx;

    private readonly int _chunkDimension;
    private readonly int _tileCount;
    private readonly int _tileSize;

    private readonly ushort _meshId;
    private readonly ushort _vertexBufferId;
    private readonly ushort _indexBufferId;

    private readonly TileData[] _tileData;

    private bool _disposed = false;

    public TilemapChunkMesh(IGraphicsDevice graphics, int chunkDimension, int tileSize)
    {
        _ctx = graphics.Ctx;
        _graphics = graphics;
        _chunkDimension = chunkDimension;
        _tileCount = _chunkDimension * _chunkDimension;
        _tileSize = tileSize;

        _tileData = new TileData[_tileCount];

        CreateTileData();
        CreateVertexBufferData();
        CreateIndexBufferData();

        var meshData = new MeshDescriptor<Vertex2D, ushort>
        {
            VertexBuffer = new MeshDataBufferDescriptor<Vertex2D>(BufferUsage.DynamicDraw, Vertices),
            IndexBuffer = new MeshDataBufferDescriptor<ushort>(BufferUsage.DynamicDraw, Indices),
            VertexPointers =
            [
                VertexAttributeDescriptor.Make<Vertex2D>("aPos", nameof(Vertex2D.Position)),
                VertexAttributeDescriptor.Make<Vertex2D>("aTex", nameof(Vertex2D.Texture))
            ],
            DrawCount = (uint)(_tileCount * IndicesPerTile)
        };

        var meshResult = _graphics.CreateMesh(meshData);

        _meshId = meshResult.MeshId;
        _vertexBufferId = meshResult.VertexBufferId;
        _indexBufferId = meshResult.IndexBufferId;
    }

    public TileChunkBuildResult BuildTilemapMesh()
    {
        return new TileChunkBuildResult(_meshId, (uint)(_tileCount * IndicesPerTile));
    }

    private void CreateTileData()
    {
        for (int y = 0; y < _chunkDimension; y++)
        {
            int rowStart = y * _chunkDimension;
            for (int x = 0; x < _chunkDimension; x++)
            {
                _tileData[rowStart + x] = new TileData((ushort)(x % 3), (ushort)(y % 3));
            }
        }
    }

    private void CreateVertexBufferData()
    {
        var vertices = Vertices;
        var w = _tileSize;

        var atlas = new SpriteAtlas(10, 16);
        var uvScale = atlas.Scale;

        for (int y = 0; y < _chunkDimension; y++)
        {
            int rowStart = y * _chunkDimension;
            for (int x = 0; x < _chunkDimension; x++)
            {
                ref var tile = ref _tileData[rowStart + x];
                int tileId = rowStart + x;
                int vi = tileId * 4;

                var uvOffset = atlas.GetOffset(tile.AtlasX, tile.AtlasY);
                float px = x * w;
                float py = y * w;

                var u0 = uvOffset.X;
                var v0 = uvOffset.Y;
                var u1 = uvOffset.X + uvScale.X;
                var v1 = uvOffset.Y + uvScale.Y;

                //top left origin
                // Bottom-left
                vertices[vi + 0] = new Vertex2D(px, py, u0, v0);
                // Bottom-right
                vertices[vi + 1] = new Vertex2D(px + w, py, u1, v0);
                // Top-left
                vertices[vi + 2] = new Vertex2D(px, py + w, u0, v1);
                // Top-right
                vertices[vi + 3] = new Vertex2D(px + w, py + w, u1, v1);
            }
        }
    }

    private void CreateIndexBufferData()
    {
        var tileCount = (ushort)_tileCount;
        var indices = Indices;
        for (ushort i = 0; i < tileCount; i++)
        {
            ushort vi = (ushort)(i * 4);
            ushort ii = (ushort)(i * 6);
            indices[ii + 0] = (ushort)(vi + 0);
            indices[ii + 1] = (ushort)(vi + 1);
            indices[ii + 2] = (ushort)(vi + 2);
            indices[ii + 3] = (ushort)(vi + 2);
            indices[ii + 4] = (ushort)(vi + 1);
            indices[ii + 5] = (ushort)(vi + 3);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _graphics.RemoveResource(_meshId);
        _disposed = true;
    }
}