using System.Numerics;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class SceneDrawProducer : IDrawCommandProducer
{
    private CommandProducerContext _context = null!;

    private RenderGlobalSnapshot _snapshot;


    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize()
    {
    }

    public void SetSceneGlobals(in RenderGlobalSnapshot snapshot) => _snapshot = snapshot;

    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
    }

    public void EndTick()
    {
    }


    public void EmitFrame(float alpha, IRenderPipeline submitter)
    {
        if (!_snapshot.Skybox.CubemapId.IsValid()) return;

        var skybox = _snapshot.Skybox;
        var cmd = new DrawCommandSkybox(
            textureId: skybox.CubemapId,
            shaderId: skybox.ShaderId,
            transform: Matrix4x4.Identity
        );


        var meta = new DrawCommandMeta( DrawCommandId.Skybox, RenderTargetId.Scene, DrawCommandQueue.Skybox);

        submitter.SubmitDraw(in cmd, in meta);
    }
}