using System.Runtime.InteropServices;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core.Features.Effects;

public sealed class LightFeature : GameFeature, IDrawableFeature<LightFeatureDrawData>
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override bool IsUpdateable => true;

    private LightFeatureDrawData _drawData = new();

    private List<LightEntity> _lights = [];

    public override void Initialize()
    {
    }

    public override void CollectFrame(ISceneNodeCollector collector)
    {
        _lights.Clear();

        var nodes = collector.GetSceneNodes<LightBehaviour>();
        foreach (var node in nodes)
        {
            var behaviour = (LightBehaviour)node.Behaviour;
            _lights.Add(new LightEntity(behaviour.Position, behaviour.Color, behaviour.Radius, behaviour.Intensity));
        }
    }


    public override void UpdateTick(int tick)
    {
        var lights = CollectionsMarshal.AsSpan(_lights);
        foreach (ref var light in lights)
        {
            light.Position += light.Delta;
            if (light.Position.X < 0) light.Delta.X = 1;
            if (light.Position.X > 2048) light.Delta.X = -1;
            if (light.Position.Y < 0) light.Delta.Y = 1;
            if (light.Position.Y > 2048) light.Delta.Y = -1;
        }
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }

    public LightFeatureDrawData GetDrawables()
    {
        _drawData.Entities = _lights;
        return _drawData;
    }
}