using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Utils;

namespace ConcreteEngine.Core.Game.Terrain;

public class TilemapFeature: GameFeature
{
    public int MapDimension { get; } = 64;
    public int TileSize { get; } = 32;
    public Shader TilemapShader { get; set; } = null!;
    public Texture2D TilemapTexture { get; set; } = null!;
    public SpriteAtlas  TilemapAtlas { get; set; } = null!;

    
    public override bool IsUpdateable => true;
    
    public override void Load()
    {
        TilemapShader = Context.Assets.Get<Shader>("SpriteShader");
        TilemapTexture = Context.Assets.Get<Texture2D>("TilemapTextureAtlas");
        TilemapAtlas = new SpriteAtlas(32, TilemapTexture);
        
    }
    
    public override void Update(float dt)
    {
    }
    public override void Unload()
    {
    }
}