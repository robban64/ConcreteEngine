using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;

namespace ConcreteEngine.Core.Features;


public sealed class TerrainFeature: GameFeature
{
    
    private ITerrainDrawSink _drawSink = null!;

    private MaterialId _materialId;
    private Texture2D _heightmap = null!;
    
    public override void Initialize()
    {
        var assets = Context.GetSystem<IAssetSystem>();
        var renderer = Context.GetSystem<IRenderSystem>();
        var material = renderer.CreateMaterial("TerrainMat");
        var heightmap = assets.Get<Texture2D>("Heightmap");
        
        material.UvRepeat = 20;

        
        _drawSink = renderer.GetSink<ITerrainDrawSink>();

        _materialId = material.Id;
        _heightmap = heightmap;
    }

    public override void UpdateTick(int tick)
    {
        _drawSink.Send(new TerrainDrawData
        {
            Heightmap = _heightmap,
            MaterialId = _materialId,
            MaxHeight = 12,
            Step = 1
        });
    }
}