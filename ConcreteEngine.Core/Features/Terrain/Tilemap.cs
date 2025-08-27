namespace ConcreteEngine.Core.Features.Terrain;

public readonly struct TileData
{
    public readonly byte TileX;
    public readonly byte TileY;

    public TileData(byte tileX, byte tileY)
    {
        TileY = tileY;
        TileX = tileX;
    }
}

public class TilemapLayer
{
    private readonly TileData[] _tiles;

    public TilemapLayer(int mapSize)
    {
        _tiles = new TileData[mapSize * mapSize];
    }
}

public sealed class Tilemap
{
    private readonly int _mapSize;
    private readonly int _tileSize;

    private readonly TileData[] _tiles;

    public Tilemap(int mapSize, int tileSize)
    {
        _mapSize = mapSize;
        _tileSize = tileSize;

        _tiles = new TileData[mapSize * mapSize];
        /*
        for (int y = 0; y < mapSize; y++)
        {
            int rowStart = y * mapSize;
            for (int x = 0; x < mapSize; x++)
            {
                ref TileData t = ref _tiles[rowStart + x];
            }
        }
        */
    }
}