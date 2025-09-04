#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Scene;

#endregion

namespace ConcreteEngine.Core.Rendering;


public interface ILightDrawSink : IDrawSink
{
    void Send(ReadOnlySpan<LightComponent> payload);
    void SendSingle(in LightComponent payload);

}
public sealed class LightProducer : IDrawCommandProducer, ILightDrawSink
{
    private CommandProducerContext _context = null!;
    
    private List<LightComponent> _lights = new();

    public void Send(ReadOnlySpan<LightComponent> payload)
    {
        _lights.AddRange(payload);
    }

    public void SendSingle(in LightComponent payload)
    {
        _lights.Add(payload);
    }

    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }
    
    public void Initialize()
    {
    }
    
    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
        _lights.Clear();
    }

    public void EndTick()
    {
    }
    
    public void EmitFrame(float alpha, RenderPipeline submitter)
    {
        var lights = CollectionsMarshal.AsSpan(_lights);
        foreach (ref var light in lights)
        {
            var cmd = new DrawCommandLight(light.Position, light.Color, light.Radius, light.Intensity);
            var meta = DrawCommandMeta.Make2D(DrawCommandId.Light, DrawCommandTag.Effect2D, RenderTargetId.SceneLight);

            submitter.SubmitDraw(in cmd, in meta);
        }
    }
}