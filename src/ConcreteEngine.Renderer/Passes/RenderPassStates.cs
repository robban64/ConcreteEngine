using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Renderer.Passes;

public struct PassMutationState
{
    private const byte LinearFilterBit = 1 << 0;
    private const byte HasClearBit = 1 << 1;
    private const byte HasStateBit = 1 << 2;
    private const byte HasFboBit = 1 << 3;
    private const byte HasShaderBit = 1 << 4;


    public FrameBufferId TargetFboId;
    public ShaderId ShaderId;
    public GfxPassClear ClearColor;
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
    public readonly bool HasClear => (_mask & HasClearBit) != 0;
    public readonly bool HasPassState => (_mask & HasStateBit) != 0;
    public readonly bool HasTarget => (_mask & HasFboBit) != 0;
    public readonly bool HasShader => (_mask & HasShaderBit) != 0;

    public bool LinearFilter
    {
        readonly get => (_mask & LinearFilterBit) != 0;
        set => _mask = value ? (byte)(_mask | LinearFilterBit) : (byte)(_mask & ~LinearFilterBit);
    }

    public void WithClear(GfxPassClear clearColor)
    {
        ClearColor = clearColor;
        _mask |= HasClearBit;
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
    GfxPassClear passClear,
    GfxPassState passState,
    ShaderId shaderId = default,
    FrameBufferId targetFboId = default,
    bool linearFilter = false
)
{
    public readonly GfxPassClear PassClear = passClear;
    public readonly GfxPassState PassState = passState;
    public readonly ShaderId ShaderId = shaderId;
    public readonly FrameBufferId TargetFboId = targetFboId;
    public readonly bool LinearFilter = linearFilter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderPassState FromMutation(in PassMutationState m) =>
        new(passClear: m.HasClear ? m.ClearColor : PassClear,
            passState: m.HasPassState ? m.PassState : PassState,
            targetFboId: m.HasTarget ? m.TargetFboId : TargetFboId,
            shaderId: m.HasShader ? m.ShaderId : ShaderId,
            linearFilter: m.HasLinearFilter ? m.LinearFilter : LinearFilter
        );


    public static RenderPassState MakeSceneMsaa() =>
        new(passClear: GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue), passState: GfxPassState.MakeScene());

    public static RenderPassState MakeResolve() =>
        new(passClear: GfxPassClear.MakeNoClear(), passState: GfxPassState.MakeOff(), linearFilter: true);

    public static RenderPassState MakePostProcess(ShaderId shaderId) =>
        new(passClear: GfxPassClear.MakeColorClear(Color4.Black), passState: GfxPassState.MakePostProcess(),
            shaderId: shaderId);

    public static RenderPassState MakeScreen(ShaderId shaderId) =>
        new(passClear: GfxPassClear.MakeColorClear(Color4.Black), passState: GfxPassState.MakeScreen(),
            shaderId: shaderId);

    public static RenderPassState MakeShadow() =>
        new(passClear: GfxPassClear.MakeDepthClear(), passState: GfxPassState.MakeShadow());

    public static RenderPassState MakeSceneEffect() =>
        new(passClear: GfxPassClear.MakeNoClear(), passState: GfxPassState.MakeSceneEffect());
}