#region

using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Rendering;

internal sealed class RenderStateContext
{
    private RenderFrameInfo _frameInfo;
    private RenderRuntimeParams _frameParams;
    public ref readonly RenderFrameInfo CurrentFrameInfo => ref _frameInfo;
    public ref readonly RenderRuntimeParams CurrentRuntimeParams => ref _frameParams;

    public required RenderSceneSnapshot Snapshot { get; init; }

    public required RenderView View { get; init; }

    public void SetCurrentFrameInfo(in RenderFrameInfo frameInfo, in RenderRuntimeParams frameParams)
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
    public required BatcherRegistry Batchers { get; init; }
}