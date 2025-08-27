using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Pipeline;

namespace ConcreteEngine.Core.Game.Effects;


public sealed class LightFeature : IGameFeature, IDrawableFeature<LightFeatureDrawData>
{
    public bool IsDrawable => true;
    public int DrawOrder => 0;

    private LightFeatureDrawData _drawData = new();
    public bool IsUpdateable => true;
    public int Order { get; set; }
    
    private List<LightEntity> _lights = new(64);

    
    public void Load(GameFeatureContext context, int order)
    {
        Order = order;

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

    public void UpdateTick(int tick)
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

    public LightFeatureDrawData GetDrawables()
    {
        _drawData.Entities =  _lights;
        return _drawData;
    }

    public void Unload()
    {
    }

}