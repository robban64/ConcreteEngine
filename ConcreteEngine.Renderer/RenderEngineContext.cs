using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Renderer;

internal sealed class RenderStateContext
{
    private FrameInfo _frameInfo;
    private RenderRuntimeParams _frameParams;
    public ref readonly FrameInfo CurrentFrameInfo => ref _frameInfo;
    public ref readonly RenderRuntimeParams CurrentRuntimeParams => ref _frameParams;

    public MeshId FsqMesh { get; init; }

    public RenderParamsSnapshot Snapshot { get; set; } = null!;
    public required RenderCamera Camera { get; init; }

    public void SetCurrentFrameInfo(in FrameInfo frameInfo, in RenderRuntimeParams frameParams)
    {
        _frameInfo = frameInfo;
        _frameParams = frameParams;
    }
}

internal sealed class RenderEngineContext
{
    public required GfxContext Gfx { get; init; }
    public required RenderRegistry Registry { get; init; }
    public required DrawCommandPipeline CommandPipeline { get; init; }
    public required RenderPassPipeline PassPipeline { get; init; }
}