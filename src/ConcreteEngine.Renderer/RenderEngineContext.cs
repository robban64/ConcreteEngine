using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

internal sealed class RenderStateContext
{
    public FrameInfo FrameInfo;
    public RenderRuntimeParams FrameParams;

    public MeshId FsqMesh;

    public RenderParamsSnapshot Snapshot = null!;
    public required RenderCamera Camera;

    public void SetCurrentFrameInfo(in FrameInfo frameInfo, in RenderRuntimeParams frameParams)
    {
        FrameInfo = frameInfo;
        FrameParams = frameParams;
    }
}

internal sealed class RenderEngineContext
{
    public required GfxContext Gfx { get; init; }
    public required RenderRegistry Registry { get; init; }
    public required DrawCommandPipeline CommandPipeline { get; init; }
    public required RenderPassPipeline PassPipeline { get; init; }
}