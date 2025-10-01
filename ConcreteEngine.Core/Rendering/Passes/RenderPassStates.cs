#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;
/*
public readonly record struct PassCommandState()
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue);

    public GfxPassState PassState { get; init; }
        = new(DepthTest: true, DepthWrite: true, Cull: true, Blend: false, Scissor: false, FramebufferSrgb: true);
}

public interface IPassApplyState
{
    public ShaderId? TargetShaderId { get; }
    public FrameBufferId TargetFboId { get; }
    public GfxPassClear ClearColor { get; }
    public GfxPassState PassState { get; }
}

public abstract class RenderPassState
{
    public ShaderId? TargetShaderId { get; set; }
    public FrameBufferId TargetFboId { get; set; }
    public GfxPassClear ClearColor { get; set; }
    public GfxPassState PassState { get; set; }

   // protected abstract void ResetState();
}

public sealed class DrawPassState() : RenderPassState
{
    public required int Samples { get; init; }
}

public sealed class TargetResolvePassState : RenderPassState
{
    public FrameBufferId ResolveToId { get; set; }
    public bool LinearFilter { get;set;  }
}

public sealed class EffectPassState : RenderPassState
{
    public List<TextureId> SourceTextureIds { get; } = new();
    public RenderBufferId? DepthBufferId { get; set; }
}

public sealed class ScreenPassState : RenderPassState
{
    public List<TextureId> SourceTextureIds { get; } = new();
    public RenderBufferId? DepthBufferId { get; set; }
}

*/

public interface IRenderPassState
{
    GfxPassClear ClearColor { get; }
    GfxPassState PassState { get; }
}

public interface IRenderPassState<out TState> : IRenderPassState where TState : unmanaged
{
    TState FromMutation(in PassMutationState m);
}

public readonly record struct PassMutationState(
    GfxPassClear? ClearColor = null,
    GfxPassState? PassState = null,
    FrameBufferId? TargetFboId = null,
    ShaderId? ShaderId = null,
    int? Samples = null,
    bool? LinearFilter = null
)
{
    public static PassMutationState MakeTargetMut(FrameBufferId fboId) => new(TargetFboId: fboId);
}

public readonly record struct RenderPassState(
    GfxPassClear ClearColor,
    GfxPassState PassState,
    ShaderId ShaderId = default,
    FrameBufferId TargetFboId = default,
    int Samples = 0,
    bool LinearFilter = false
)
{
    public RenderPassState FromMutation(in PassMutationState m) =>
        new(ClearColor: m.ClearColor ?? ClearColor,
            PassState: m.PassState ?? PassState,
            TargetFboId: m.TargetFboId ?? TargetFboId,
            ShaderId: m.ShaderId ?? ShaderId,
            Samples: m.Samples ?? Samples,
            LinearFilter: m.LinearFilter ?? LinearFilter
        );
}

public readonly record struct EmptyState(GfxPassClear ClearColor = default, GfxPassState PassState = default)
    : IRenderPassState;

public readonly record struct ScenePassState() : IRenderPassState<ScenePassState>
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue);

    public GfxPassState PassState { get; init; }
        = new(DepthTest: true, DepthWrite: true, Cull: true, Blend: false, Scissor: false, FramebufferSrgb: true);

    public int Samples { get; init; } = 4;

    public ShaderId ShaderId { get; init; } = default;

    public ScenePassState FromMutation(in PassMutationState m) =>
        this with
        {
            ClearColor = m.ClearColor ?? ClearColor,
            PassState = m.PassState ?? PassState,
            ShaderId = m.ShaderId ?? ShaderId
        };
}

public readonly record struct PostPassState(ShaderId ShaderId) : IRenderPassState<PostPassState>
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);

    public GfxPassState PassState { get; init; } = GfxPassState.MakePostProcess();

    public PostPassState FromMutation(in PassMutationState m) =>
        this with
        {
            ClearColor = m.ClearColor ?? ClearColor,
            PassState = m.PassState ?? PassState,
            ShaderId = m.ShaderId ?? ShaderId
        };
}

public readonly record struct ScreenPassState(ShaderId PresentShaderId) : IRenderPassState<ScreenPassState>
{
    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);
    public GfxPassState PassState { get; init; } = GfxPassState.MakeScreen();

    public ScreenPassState FromMutation(in PassMutationState m) =>
        this with
        {
            ClearColor = m.ClearColor ?? ClearColor,
            PassState = m.PassState ?? PassState,
            PresentShaderId = m.ShaderId ?? PresentShaderId
        };
}

public readonly record struct ResolvePassState() : IRenderPassState<ResolvePassState>
{
    public FrameBufferId TargetFboId { get; init; } = default;

    public GfxPassClear ClearColor { get; init; } = GfxPassClear.MakeColorClear(Color4.Black);
    public GfxPassState PassState { get; init; } = GfxPassState.MakeOff();
    public bool LinearFilter { get; init; } = true;

    public ResolvePassState FromMutation(in PassMutationState m) =>
        this with
        {
            ClearColor = m.ClearColor ?? ClearColor,
            PassState = m.PassState ?? PassState,
            TargetFboId = m.TargetFboId ?? TargetFboId,
            LinearFilter = m.LinearFilter ?? LinearFilter
        };
}