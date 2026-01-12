using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

internal sealed class RenderStateContext
{
    public RenderParamsSnapshot Snapshot = new();
    public required RenderCamera Camera;

    public RenderFrameArgs RenderFrameArgs;
    public MeshId FsqMesh;
}

internal sealed class RenderProgramContext
{
    public required GfxContext Gfx { get; init; }
    public required RenderRegistry Registry { get; init; }
    public required DrawCommandPipeline CommandPipeline { get; init; }
    public required RenderPassPipeline PassPipeline { get; init; }
}