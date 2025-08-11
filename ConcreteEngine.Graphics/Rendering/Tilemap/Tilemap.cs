namespace ConcreteEngine.Graphics.Rendering.Tilemap;

internal class Tilemap
{
    private const int MinMapSize = 16;
    private const int MaxMapSize = 1024 * 4;

    private const int MinTileSize = 16;
    private const int MaxTileSize = 128;

    private const int ChunkSize = 64;

    private IGraphicsDevice _device;

    public int MapSize { get; }
    public int TileSize { get; }

    public Tilemap(IGraphicsDevice graphicsDevice, int mapSize, int tileSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mapSize, MinMapSize, nameof(mapSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mapSize, MaxMapSize, nameof(mapSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(tileSize, MinTileSize, nameof(tileSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tileSize, MaxTileSize, nameof(tileSize));

        if (mapSize % 2 != 0) throw new ArgumentException("mapSize must be even");

        MapSize = mapSize;
        TileSize = tileSize;
    }
}