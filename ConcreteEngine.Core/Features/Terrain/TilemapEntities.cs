using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Features.Terrain;

public sealed class TilemapDrawData
{
    public MaterialId MaterialId { get; set; }
    public int MapDimension { get; set; } = 64;
    public int TileSize { get; set; } = 32;

    public int Count { get; set; } = 0;
}