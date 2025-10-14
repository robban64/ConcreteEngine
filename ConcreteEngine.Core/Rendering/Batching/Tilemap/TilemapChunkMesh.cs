#region

using System.Numerics;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Batching;

internal sealed class TilemapChunkMesh : IDisposable
{
    private const int VerticesPerTile = 4;
    private const int IndicesPerTile = 6;
    private const int ChunkSize = 64;

    //Static before
    private readonly Vertex2D[] Vertices =
        new Vertex2D[ChunkSize * ChunkSize * VerticesPerTile];

    //Static before
    private readonly ushort[] Indices =
        new ushort[ChunkSize * ChunkSize * IndicesPerTile];

    private readonly GfxContext _gfx;

    private readonly int _chunkDimension;
    private readonly int _tileCount;
    private readonly int _tileSize;

    private readonly MeshId _meshId;
    private readonly VertexBufferId _vertexBufferId;
    private readonly IndexBufferId _indexBufferId;

    private readonly TileDrawItem[] _tileData;

    private bool _disposed = false;

    public TilemapChunkMesh(GfxContext gfx, int chunkDimension, int tileSize)
    {
        _gfx = gfx;
        _chunkDimension = chunkDimension;
        _tileCount = _chunkDimension * _chunkDimension;
        _tileSize = tileSize;

        _tileData = new TileDrawItem[_tileCount];

        CreateTileData();
        CreateVertexBufferData();
        CreateIndexBufferData();

        var drawCount = _tileCount * IndicesPerTile;

        var props = MeshDrawProperties.MakeTriElemental(size: DrawElementSize.UnsignedShort, drawCount: drawCount);
        var builder = gfx.Meshes.StartUploadBuilder(in props);
        builder.UploadVertices<Vertex2D>(Vertices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        builder.UploadIndices<ushort>(Indices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var attribBuilder = new VertexAttributeMaker<Vertex2D>();
        builder.AddAttribute(attribBuilder.Make<Vector2>());
        builder.AddAttribute(attribBuilder.Make<Vector2>());

        var meshId = builder.Finish();

        var meshLayout = gfx.ResourceContext.Repository.MeshRepository.Get(meshId);
        _vertexBufferId = meshLayout.GetVertexBufferIds()[0];
        _indexBufferId = meshLayout.IndexBufferId;
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
                var idx = x % 3 == 0 && y % 3 == 0 ? 51 : 55;
                _tileData[rowStart + x] = new TileDrawItem((ushort)idx);
            }
        }

        var middle = _chunkDimension / 2;
        for (int y = 0; y < _chunkDimension; y++)
        {
            int rowStart = y * _chunkDimension;
            _tileData[rowStart + middle - 2] = new TileDrawItem(10);
            _tileData[rowStart + middle - 1] = new TileDrawItem(11);
            _tileData[rowStart + middle] = new TileDrawItem(11);
            _tileData[rowStart + middle + 1] = new TileDrawItem(11);
            _tileData[rowStart + middle + 2] = new TileDrawItem(12);
        }
    }

    private void CreateVertexBufferData()
    {
        var vertices = Vertices;
        var w = _tileSize;

        var atlas = new SpriteAtlas(new Vector2D<int>(_tileSize, _tileSize), new Vector2D<int>(320, 512));

        for (int y = 0; y < _chunkDimension; y++)
        {
            int rowStart = y * _chunkDimension;
            for (int x = 0; x < _chunkDimension; x++)
            {
                ref var tile = ref _tileData[rowStart + x];
                int vi = (rowStart + x) * 4;

                float px = x * _tileSize;
                float py = y * _tileSize;

                var (u0, v0, u1, v1) = atlas.GetUvRect(tile.TextureIndex);

                vertices[vi + 0] = new Vertex2D(px, py, u0, v0);
                vertices[vi + 1] = new Vertex2D(px + _tileSize, py, u1, v0);
                vertices[vi + 2] = new Vertex2D(px, py + _tileSize, u0, v1);
                vertices[vi + 3] = new Vertex2D(px + _tileSize, py + _tileSize, u1, v1);
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
        _gfx.ResourceContext.Disposer.EnqueueRemoval(_meshId, false);
        _disposed = true;
    }
}