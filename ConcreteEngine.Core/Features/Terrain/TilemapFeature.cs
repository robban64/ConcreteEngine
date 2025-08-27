#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Features.Terrain;



public class TilemapFeature : GameFeature, IDrawableFeature<TilemapDrawData>
{

    private TilemapDrawData _drawData = new();
    public Shader TilemapShader { get; set; } = null!;
    public Texture2D TilemapTexture { get; set; } = null!;
    
    private TilemapDrawData _tilemap;
    
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override void Initialize()
    {
        TilemapShader = Context.AssetSystem.Get<Shader>("SpriteShader");
        TilemapTexture = Context.AssetSystem.Get<Texture2D>("TilemapTextureAtlas");
        _drawData.Shader =  TilemapShader.ResourceId;
        _drawData.Texture =  TilemapTexture.ResourceId;
    }

    public override void UpdateTick(int tick)
    {
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }


    public TilemapDrawData GetDrawables() => _drawData;

}