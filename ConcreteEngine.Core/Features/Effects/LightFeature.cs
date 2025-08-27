using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core.Features.Effects;


public sealed class LightFeature : GameFeature, IDrawableFeature<LightFeatureDrawData>
{
    
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override bool IsUpdateable => true;

    private LightFeatureDrawData _drawData = new();
    
    private List<LightEntity> _lights = new(64);

    public override void Initialize()
    {
        var r = Random.Shared;
        for (int i = 0; i < 64; i++)
        {
            var l = new LightEntity(new(r.Next(0, 2048), r.Next(0, 2048)),
                new(0.7f, 0.8f, 1.0f), r.Next(100, 280),
                r.NextSingle() * 2);
            
            l.Delta =new Vector2(r.NextSingle(), r.NextSingle());
            _lights.Add(l);
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
        _drawData.Entities =  _lights;
        return _drawData;
    }


}