using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Utils;

namespace ConcreteEngine.Core.Game.Terrain;

public class TilemapFeature: IGameFeature
{
    public int MapDimension { get; } = 64;
    public int TileSize { get; } = 32;
    public Shader TilemapShader { get; set; } = null!;
    public Texture2D TilemapTexture { get; set; } = null!;
    public SpriteAtlas  TilemapAtlas { get; set; } = null!;

    
    public bool IsUpdateable => true;
    public int Order { get; set; }

    public void Load(GameFeatureContext context)
    {
        var assets = context.GetSystem<AssetSystem>();
        TilemapShader = assets.Get<Shader>("SpriteShader");
        TilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");
        TilemapAtlas = new SpriteAtlas(32, TilemapTexture);
        
    }
    
    public void UpdateTick(int tick)
    {
    }
    public void Unload()
    {
    }
}