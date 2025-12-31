using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Renderer.Passes;

public struct PassMutationState
{
    private const uint LinearFilterBit = 1u << 0;
    private const uint HasClearBit = 1u << 8;
    private const uint HasStateBit = 1u << 9;
    private const uint HasFboBit = 1u << 10;
    private const uint HasShaderBit = 1u << 11;
    private const uint HasSamplesBit = 1u << 12;

    public GfxPassState PassState;
    private uint _mask;

    public FrameBufferId TargetFboId;
    public ShaderId ShaderId;
    public GfxPassClear ClearColor;
    public byte Samples;

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
    public readonly bool HasSample => (_mask & HasSamplesBit) != 0;

    public bool LinearFilter
    {
        get => (_mask & LinearFilterBit) != 0;
        set => _mask = value ? _mask | LinearFilterBit : _mask & ~LinearFilterBit;
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

    public void WithSamples(byte count)
    {
        Samples = count;
        _mask |= HasSamplesBit;
    }
}

public readonly struct RenderPassState(
    GfxPassClear clearColor,
    GfxPassState passState,
    ShaderId shaderId = default,
    FrameBufferId targetFboId = default,
    int samples = 0,
    bool linearFilter = false
)
{
    public readonly GfxPassState PassState = passState;
    public readonly ShaderId ShaderId = shaderId;
    public readonly FrameBufferId TargetFboId = targetFboId;
    public readonly GfxPassClear ClearColor = clearColor;
    public readonly byte Samples = (byte)samples;
    public readonly bool LinearFilter = linearFilter;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderPassState FromMutation(in PassMutationState m) =>
        new(clearColor: m.HasClear ? m.ClearColor : ClearColor,
            passState: m.HasPassState ? m.PassState : PassState,
            targetFboId: m.HasTarget ? m.TargetFboId : TargetFboId,
            shaderId: m.HasShader ? m.ShaderId : ShaderId,
            samples: m.HasSample ? m.Samples : Samples,
            linearFilter: m.HasLinearFilter ? m.LinearFilter : LinearFilter
        );


    public static RenderPassState MakeSceneMsaa(int samples) =>
        new(clearColor: GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue), passState: GfxPassState.MakeScene(),
            samples: samples);

    public static RenderPassState MakeResolve() =>
        new(clearColor: GfxPassClear.MakeNoClear(),
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