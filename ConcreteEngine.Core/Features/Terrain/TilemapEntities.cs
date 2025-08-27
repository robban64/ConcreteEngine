using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Features.Terrain;


public class TilemapDrawData
{
    public ShaderId Shader = default;
    public TextureId Texture = default;
    public int MapDimension = 64;
    public int TileSize { get; } = 32;

}