#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Game.Terrain;



public class TilemapFeature : IGameFeature, IDrawableFeature<TilemapDrawData>
{
    public bool IsDrawable => true;
    public int DrawOrder => 0;

    private TilemapDrawData _drawData = new();
    public Shader TilemapShader { get; set; } = null!;
    public Texture2D TilemapTexture { get; set; } = null!;

    public bool IsUpdateable => true;
    public int Order { get; set; }

    private TilemapDrawData _tilemap;
    
    public void Load(GameFeatureContext context, int order)
    {
        Order = order;
        var assets = context.GetSystem<AssetSystem>();
        TilemapShader = assets.Get<Shader>("SpriteShader");
        TilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");
        _drawData.Shader =  TilemapShader.ResourceId;
        _drawData.Texture =  TilemapTexture.ResourceId;
    }

    public void UpdateTick(int tick)
    {
    }
    
    

    public TilemapDrawData GetDrawables() => _drawData;

    public void Unload()
    {
    }

}