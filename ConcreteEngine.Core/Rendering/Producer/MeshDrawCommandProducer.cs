using ConcreteEngine.Core.Scene;

namespace ConcreteEngine.Core.Rendering;

public sealed class MeshDrawData
{
}

public sealed class MeshDrawProducer : DrawCommandProducer<MeshDrawData>
{
    public override void OnInitialize()
    {

    }

    protected override void EmitCommands(float alpha, MeshDrawData data, DrawCommandSubmitter submitter)
    {
        foreach (var view in Context.World.Meshes.View2(Context.World.Transforms))
        {
            ref var mesh = ref view.Value1;
            ref var transform = ref view.Value2;
            
            var cmd = new DrawCommandSprite(
                meshId: mesh.MeshId,
                drawCount: mesh.DrawCount,
                materialId: mesh.MaterialId,
                transform: transform.GetTransform()
            );

            var meta = new DrawCommandMeta(DrawCommandId.Diffuse, DrawCommandTag.Mesh3D, RenderTargetId.Scene, 0);
            submitter.SubmitDraw(in cmd, in meta);

        }
    }
}