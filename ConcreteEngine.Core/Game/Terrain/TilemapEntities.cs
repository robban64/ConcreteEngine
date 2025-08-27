using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Game.Terrain;


public class TilemapDrawData
{
    public ShaderId Shader = default;
    public TextureId Texture = default;
    public int MapDimension = 64;
    public int TileSize { get; } = 32;

}