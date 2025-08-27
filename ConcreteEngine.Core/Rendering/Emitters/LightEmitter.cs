#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Game.Effects;
using ConcreteEngine.Core.Rendering.Pipeline;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;



public sealed class LightEmitter : DrawCommandEmitter<LightFeatureDrawData>
{

    protected override void EmitBatch(LightFeatureDrawData data, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        var lights = CollectionsMarshal.AsSpan(data.Entities);
        foreach (ref var light in lights)
        {
            var cmd = LightEntity.ToCmd(light);
            var meta = new DrawCommandMeta(DrawCommandId.Effect, DrawCommandTag.LightRenderer, 
                RenderTargetId.SceneLight, 0);

            submitter.SubmitDraw(in cmd, in meta);

        }

    }
}