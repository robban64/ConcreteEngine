using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Rendering;

internal sealed class RenderSystemContext
{
    public required GfxContext Gfx { get; init;}
    public required RenderRegistry Registry { get; init;}
    public required DrawCommandPipeline CommandPipeline { get; init;}
    public required RenderPassPipeline PassPipeline { get; init;}
    public required RenderSceneState Snapshot { get; init;}
    public required RenderView View { get; init; }
    public required BatcherRegistry Batchers { get; init; }

}