using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;

namespace ConcreteEngine.Core.Features;


public sealed class TerrainFeature: GameFeature, IDrawableFeature<TerrainDrawData>
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;
    public override bool IsUpdateable => true;

    private readonly TerrainDrawData _drawData = new ();

    public override void Initialize()
    {
        var assets = Context.GetSystem<IAssetSystem>();
        var renderer = Context.GetSystem<IRenderSystem>();
        var material = renderer.CreateMaterial("TerrainMat");
        var heightmap = assets.Get<Texture2D>("Heightmap");
        _drawData.MaterialId = material.Id;
        _drawData.Heightmap = heightmap;
        _drawData.MaxHeight = 4;
    }
    
    public TerrainDrawData GetDrawables()
    {
        return _drawData;
    }

}