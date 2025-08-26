#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Game.Terrain;

public struct TilemapStruct
{
    public int MapDimension { get; } = 64;
    public int TileSize { get; } = 32;
    public ShaderId Shader = default;
    public TextureId Texture = default;

    public TilemapStruct()
    {
    }
}

public class TilemapFeature : IGameFeature, IDrawableFeature<TilemapStruct>
{
    public bool IsDrawable => true;
    public int DrawOrder => 0;

    public int MapDimension { get; } = 64;
    public int TileSize { get; } = 32;
    public Shader TilemapShader { get; set; } = null!;
    public Texture2D TilemapTexture { get; set; } = null!;


    public bool IsUpdateable => true;
    public int Order { get; set; }

    private TilemapStruct _tilemap;

    private readonly TilemapStruct[] _tilemaps = new TilemapStruct[1];

    public void Load(GameFeatureContext context, int order)
    {
        Order = order;
        var assets = context.GetSystem<AssetSystem>();
        TilemapShader = assets.Get<Shader>("SpriteShader");
        TilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");
    }

    public void UpdateTick(int tick)
    {
    }

    public void Unload()
    {
    }

    public ReadOnlySpan<TilemapStruct> GetDrawables()
    {
        _tilemaps[0] = _tilemap;
        return _tilemaps;
    }
}