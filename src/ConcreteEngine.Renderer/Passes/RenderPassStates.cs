using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Renderer.Passes;

public struct PassMutationState
{
    private const byte LinearFilterBit = 1 << 0;
    private const byte HasStateBit = 1 << 1;
    private const byte HasFboBit = 1 << 2;
    private const byte HasShaderBit = 1 << 3;


    public FrameBufferId TargetFboId;
    public ShaderId ShaderId;
    public GfxPassState PassState;

    private byte _mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PassMutationState MutateTarget(FrameBufferId fboId)
    {
        var state = new PassMutationState();
        state.WithTarget(fboId);
        return state;
    }

    public readonly bool HasLinearFilter => (_mask & LinearFilterBit) != 0;
    public readonly bool HasState => (_mask & HasStateBit) != 0;
    public readonly bool HasTarget => (_mask & HasFboBit) != 0;
    public readonly bool HasShader => (_mask & HasShaderBit) != 0;

    public bool LinearFilter
    {
        readonly get => (_mask & LinearFilterBit) != 0;
        set => _mask = value ? (byte)(_mask | LinearFilterBit) : (byte)(_mask & ~LinearFilterBit);
    }

    public void WithState(GfxPassState stateColor)
    {
        PassState = stateColor;
        _mask |= HasStateBit;
    }

    public void WithShader(ShaderId id)
    {
        ShaderId = id;
        _mask |= HasShaderBit;
    }

    public void WithTarget(FrameBufferId id)
    {
        TargetFboId = id;
        _mask |= HasFboBit;
    }

}

public readonly struct RenderPassState(
    GfxPassState passState,
    ShaderId shaderId = default,
    FrameBufferId targetFboId = default,
    bool linearFilter = false
)
{
    public readonly GfxPassState PassState = passState;
    public readonly ShaderId ShaderId = shaderId;
    public readonly FrameBufferId TargetFboId = targetFboId;
    public readonly bool LinearFilter = linearFilter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderPassState FromMutation(in PassMutationState m) =>
        new(passState: m.HasState ? m.PassState : PassState,
            targetFboId: m.HasTarget ? m.TargetFboId : TargetFboId,
            shaderId: m.HasShader ? m.ShaderId : ShaderId,
            linearFilter: m.HasLinearFilter ? m.LinearFilter : LinearFilter
        );


    public static RenderPassState MakeSceneMsaa() =>
        new(passState: GfxPassState.MakeColorDepthClear(Color4.CornflowerBlue, DepthTest | DepthWrite | Cull | Srgb | ColorMask | Ac2));

    public static RenderPassState MakeResolve() =>
        new(passState: GfxPassState.MakeNoClear(ColorMask), linearFilter: true);

    public static RenderPassState MakePostProcess(ShaderId shaderId) =>
        new(GfxPassState.MakeColorClear(Color4.Black, ColorMask | Srgb),
            shaderId: shaderId);

    public static RenderPassState MakeScreen(ShaderId shaderId) =>
        new(passState: GfxPassState.MakeColorClear(Color4.Black, ColorMask | Srgb),
            shaderId: shaderId);

    public static RenderPassState MakeShadow() =>
        new(passState: GfxPassState.MakeDepthClear(DepthTest | DepthWrite | Cull | Srgb | PolygonOffset | Ac2));

    public static RenderPassState MakeSceneEffect() =>
        new(passState: GfxPassState.MakeNoClear(Blend | Cull | Srgb | ColorMask | Ac2));
    
}