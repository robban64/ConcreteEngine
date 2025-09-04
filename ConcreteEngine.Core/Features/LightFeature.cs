#region

using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class LightFeature : GameFeature
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private ILightDrawSink _drawSink = null!;

    public override void Initialize()
    {
        _drawSink = Context.GetSystem<IRenderSystem>().GetSink<ILightDrawSink>();
    }

    public override void UpdateTick(int tick)
    {
        var lights = Context.World.Lights;
        foreach (var entry in lights)
        {
            ref var light = ref entry.Value;
            _drawSink.SendSingle(in light);
        }
    }
}