#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering.Pipeline;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class LightEmitter : DrawCommandEmitter<DrawCommandLight>
{
    private List<DrawCommandLight> _lights = new(64);
    private float tick = 0;

    public LightEmitter()
    {
        var r = Random.Shared;
        for (int i = 0; i < 64; i++)
        {
            var l = new DrawCommandLight(new(r.Next(0, 2048), r.Next(0, 2048)),
                new(r.NextSingle(), r.NextSingle(), r.NextSingle()), r.Next(40, 200),
                r.NextSingle() * 2);
            _lights.Add(l);
        }
    }

    protected override void EmitBatch(ReadOnlySpan<DrawCommandLight> entities, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        var lightSpan = CollectionsMarshal.AsSpan(_lights);
        for (int i = 0; i < lightSpan.Length; i++)
        {
            ref var light = ref lightSpan[i];
            var m = new DrawCommandMeta(DrawCommandId.Effect, DrawCommandTag.LightRenderer, RenderTargetId.SceneLight,
                0);

            var td = tick > 0 ? 1 : -1;
            light.Position.Y += 2f * td;
            light.Color += new Vector3(0.006f, 0.001f, 0.002f);
            if (light.Color.X > 1) light.Color.X = 0;
            if (light.Color.Y > 1) light.Color.Y = 0;
            if (light.Color.Z > 1) light.Color.Z = 0;


            submitter.SubmitDraw(in light, in m);
        }

        tick += 0.1f;

        if (tick > 10)
            tick = -10;
    }
}