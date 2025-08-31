using System.Runtime.InteropServices;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core.Features;


public sealed class LightFeatureDrawData
{
    public List<LightEntity> Entities { get; set; } = [];
}

public sealed class LightFeature : GameFeature, IDrawableFeature<LightFeatureDrawData>
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override bool IsUpdateable => true;

    private LightFeatureDrawData _drawData = new();

    public override void Initialize()
    {
    }

    public override void UpdateTick(int tick)
    {
        _drawData.Entities.Clear();
        
        var lights = Context.World.Lights;
        foreach (var entry in lights)
        {
            ref var light = ref entry.Value;
            _drawData.Entities.Add(light);
        }
    }

    public LightFeatureDrawData GetDrawables()
    {
        return _drawData;
    }
}