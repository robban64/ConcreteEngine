#region

using ConcreteEngine.Core.Engine.RenderingSystem;
using ConcreteEngine.Core.Engine.RenderingSystem.Producers;
using ConcreteEngine.Core.Rendering;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class MeshEntityFeature : GameFeature
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private int _idx = 0;


    public override void Initialize()
    {
    }

    public override void UpdateTick(int tick)
    {
        return;
        _idx = 0;

        var world = Context.World;

        foreach (var view in world.Meshes.View2(world.Transforms))
        {
            ref var mesh = ref view.Value1;
            ref var transform = ref view.Value2;
            //_drawSink.SendSingle(new MeshDrawEntity(mesh.MeshId, mesh.MaterialId, ref transform));
        }
    }
}