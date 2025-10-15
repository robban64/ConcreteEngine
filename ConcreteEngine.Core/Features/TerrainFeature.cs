#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Producers;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class TerrainFeature : GameFeature
{
    private ITerrainDrawSink _drawSink = null!;

    private MaterialId _materialId;
    private Texture2D _heightmap = null!;

    public override void Initialize()
    {
        var assets = Context.GetSystem<IAssetSystem>().Store;
        var renderer = Context.GetSystem<IRenderSystem>();
        var material = renderer.CreateMaterial("TerrainMat");
        var heightmap = assets.GetByName<Texture2D>("Heightmap");

        material.UvRepeat = 28;
        material.Shininess = 10;
        material.SpecularStrength = 0.04f;

        _drawSink = renderer.GetSink<ITerrainDrawSink>();

        _materialId = material.Id;
        _heightmap = heightmap;
    }

    public override void UpdateTick(int tick)
    {
        _drawSink.Send(new TerrainDrawData
        {
            Heightmap = _heightmap, MaterialId = _materialId, MaxHeight = 12, Step = 1
        });
    }
}