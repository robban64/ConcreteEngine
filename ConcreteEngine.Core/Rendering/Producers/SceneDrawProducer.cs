#region

using System.Numerics;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.State;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public sealed class SceneDrawProducer : IDrawCommandProducer
{
    private CommandProducerContext _context = null!;

    private RenderSceneState _snapshot;


    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize()
    {
    }

    public void SetSceneGlobals(in RenderSceneState snapshot) => _snapshot = snapshot;

    public void BeginTick(in UpdateTickInfo tick)
    {
    }

    public void EndTick()
    {
    }


    public void EmitFrame(float alpha, in RenderSceneState snapshot, DrawCommandBuffer submitter)
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
}