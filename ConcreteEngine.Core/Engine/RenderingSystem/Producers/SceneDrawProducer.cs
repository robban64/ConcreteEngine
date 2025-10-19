#region

#endregion

namespace ConcreteEngine.Core.Engine.RenderingSystem.Producers;
/*
public sealed class SceneDrawProducer : IDrawCommandProducer
{
    private CommandProducerContext _context = null!;

    private RenderSceneSnapshot _snapshot;


    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize()
    {
    }

    public void SetSceneGlobals(RenderSceneSnapshot snapshot) => _snapshot = snapshot;

    public void BeginTick(in UpdateTickInfo tick)
    {
    }

    public void EndTick()
    {
    }


    public void EmitFrame(float alpha, in RenderSceneSnapshot snapshot, DrawCommandBuffer submitter)
    {
        if (_snapshot.Skybox.MaterialId.Id == 0) return;

        var skybox = _snapshot.Skybox;
        var cmd = new DrawCommand(
            meshId: _context.Gfx.Primitives.SkyboxCube,
            materialId: _snapshot.Skybox.MaterialId
        );


        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, PassMask.Main);

        submitter.SubmitDraw(cmd, meta, new DrawTransformPayload(Matrix4x4.Identity));
    }
}*/