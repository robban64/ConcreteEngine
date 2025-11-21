#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Passes;

public readonly struct PassMutationState(
    GfxPassClear? clearColor = null,
    GfxPassState? passState = null,
    FrameBufferId? targetFboId = null,
    ShaderId? shaderId = null,
    int? samples = null,
    bool? linearFilter = null
)
{
    public GfxPassClear? ClearColor { get; init; } = clearColor;
    public GfxPassState? PassState { get; init; } = passState;
    public FrameBufferId? TargetFboId { get; init; } = targetFboId;
    public ShaderId? ShaderId { get; init; } = shaderId;
    public int? Samples { get; init; } = samples;
    public bool? LinearFilter { get; init; } = linearFilter;

    public static PassMutationState MutateTarget(FrameBufferId fboId) => new(targetFboId: fboId);
}

public readonly struct RenderPassState(
    in GfxPassClear clearColor,
    GfxPassState passState,
    ShaderId shaderId = default,
    FrameBufferId targetFboId = default,
    int samples = 0,
    bool linearFilter = false
)
{
    public readonly GfxPassClear ClearColor = clearColor;
    public readonly GfxPassState PassState = passState;
    public readonly ShaderId ShaderId = shaderId;
    public readonly FrameBufferId TargetFboId = targetFboId;
    public readonly int Samples = samples;
    public readonly bool LinearFilter = linearFilter;


    public RenderPassState FromMutation(in PassMutationState m) =>
        new(clearColor: m.ClearColor ?? ClearColor,
            passState: m.PassState ?? PassState,
            targetFboId: m.TargetFboId ?? TargetFboId,
            shaderId: m.ShaderId ?? ShaderId,
            samples: m.Samples ?? Samples,
            linearFilter: m.LinearFilter ?? LinearFilter
        );

    public static RenderPassState MakeSceneMsaa(int samples) =>
        new(clearColor: GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue), passState: GfxPassState.MakeScene(),
            samples: samples);

    public static RenderPassState MakeResolve() => new(clearColor: GfxPassClear.MakeNoClear(),
        passState: GfxPassState.MakeOff(), linearFilter: true);

    public static RenderPassState MakePostProcess(ShaderId shaderId) =>
        new(clearColor: GfxPassClear.MakeColorClear(Color4.Black), passState: GfxPassState.MakePostProcess(),
            shaderId: shaderId);

    public static RenderPassState MakeScreen(ShaderId shaderId) =>
        new(clearColor: GfxPassClear.MakeColorClear(Color4.Black), passState: GfxPassState.MakeScreen(),
            shaderId: shaderId);

    public static RenderPassState MakeShadow() =>
        new(clearColor: GfxPassClear.MakeDepthClear(), passState: GfxPassState.MakeShadow());

    public static RenderPassState MakeSceneEffect(int samples) =>
        new(clearColor: GfxPassClear.MakeNoClear(), passState: GfxPassState.MakeSceneEffect(), samples: samples);
}