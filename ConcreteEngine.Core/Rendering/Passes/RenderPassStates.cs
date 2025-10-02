#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Passes;

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

    public static RenderPassState MakeSceneMsaa(int samples) => new()
    {
        ClearColor = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue),
        PassState = GfxPassState.MakeScene(),
        Samples = samples
    };

    public static RenderPassState MakeResolve() => new()
    {
        PassState = GfxPassState.MakeOff(), LinearFilter = false
    };

    public static RenderPassState MakePostProcess(ShaderId shaderId) => new()
    {
        ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
        PassState = GfxPassState.MakePostProcess(),
        ShaderId = shaderId
    };


    public static RenderPassState MakeScreen(ShaderId shaderId) => new()
    {
        ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
        PassState = GfxPassState.MakeScreen(),
        ShaderId = shaderId
    };
}