#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Features;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class LightProducer : DrawCommandProducer<LightFeatureDrawData>
{
    protected override void EmitCommands(float alpha, LightFeatureDrawData data, DrawCommandSubmitter submitter)
    {
        var lights = CollectionsMarshal.AsSpan(data.Entities);
        foreach (ref var light in lights)
        {
            var cmd = new DrawCommandLight(light.Position, light.Color, light.Radius, light.Intensity);
            var meta = new DrawCommandMeta(DrawCommandId.Light, DrawCommandTag.Effect2D,
                RenderTargetId.SceneLight, 0);

            submitter.SubmitDraw(in cmd, in meta);
        }
    }
}