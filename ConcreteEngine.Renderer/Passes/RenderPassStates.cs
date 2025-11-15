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
    public GfxPassClear ClearColor { get; init; } = clearColor;
    public GfxPassState PassState { get; init; } = passState;
    public ShaderId ShaderId { get; init; } = shaderId;
    public FrameBufferId TargetFboId { get; init; } = targetFboId;
    public int Samples { get; init; } = samples;
    public bool LinearFilter { get; init; } = linearFilter;

    public RenderPassState FromMutation(in PassMutationState m) =>
        new(clearColor: m.ClearColor ?? ClearColor,
            passState: m.PassState ?? PassState,
            targetFboId: m.TargetFboId ?? TargetFboId,
            shaderId: m.ShaderId ?? ShaderId,
            samples: m.Samples ?? Samples,
            linearFilter: m.LinearFilter ?? LinearFilter
        );

    public static RenderPassState MakeSceneMsaa(int samples) =>
        new()
        {
            ClearColor = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue),
            PassState = GfxPassState.MakeScene(),
            Samples = samples
        };

    public static RenderPassState MakeResolve() => new() { PassState = GfxPassState.MakeOff(), LinearFilter = false };

    public static RenderPassState MakePostProcess(ShaderId shaderId) =>
        new()
        {
            ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
            PassState = GfxPassState.MakePostProcess(),
            ShaderId = shaderId
        };

    public static RenderPassState MakeScreen(ShaderId shaderId) =>
        new()
        {
            ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
            PassState = GfxPassState.MakeScreen(),
            ShaderId = shaderId
        };

    public static RenderPassState MakeShadow() =>
        new() { ClearColor = GfxPassClear.MakeDepthClear(), PassState = GfxPassState.MakeShadow() };
}