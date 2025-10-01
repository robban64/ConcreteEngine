#region

using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Commands;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class MeshEntityFeature : GameFeature
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private int _idx = 0;

    private IMeshDrawSink _drawSink = null!;

    public override void Initialize()
    {
        _drawSink = Context.GetSystem<IRenderSystem>().GetSink<IMeshDrawSink>();
    }

    public override void UpdateTick(int tick)
    {
        _idx = 0;

        var world = Context.World;

        foreach (var view in world.Meshes.View2(world.Transforms))
        {
            ref var mesh = ref view.Value1;
            ref var transform = ref view.Value2;
            _drawSink.SendSingle(new MeshDrawEntity(mesh.MeshId, mesh.MaterialId, ref transform));
        }
    }
}