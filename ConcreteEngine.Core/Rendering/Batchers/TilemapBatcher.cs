#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering.Batchers;

public class TilemapBatcher : RenderBatcher<TilemapBatchResult>
{
    private const int MinMapSize = 64;
    private const int MaxMapSize = 512;

    private const int MinTileSize = 16;
    private const int MaxTileSize = 128;

    private const int ChunkDimension = 64;

    public int MapSize { get; }
    public int TileSize { get; }

    private TilemapChunkMesh _chunk;

    public TilemapBatcher(IGraphicsDevice graphics, int mapSize, int tileSize) : base(graphics)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mapSize, MinMapSize, nameof(mapSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mapSize, MaxMapSize, nameof(mapSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(tileSize, MinTileSize, nameof(tileSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tileSize, MaxTileSize, nameof(tileSize));

        if (mapSize % 2 != 0) throw new ArgumentException("mapSize must be even");

        MapSize = mapSize;
        TileSize = tileSize;

        _chunk = new TilemapChunkMesh(graphics, 64, tileSize);
    }

    public override TilemapBatchResult BuildBatch()
    {
        var ground = _chunk.BuildTilemapMesh();
        return new TilemapBatchResult(in ground);
    }

    public override void Dispose()
    {
        _chunk.Dispose();
    }
}