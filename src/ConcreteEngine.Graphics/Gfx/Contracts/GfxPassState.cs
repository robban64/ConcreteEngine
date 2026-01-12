using System.Runtime.CompilerServices;
using static ConcreteEngine.Graphics.Gfx.Contracts.GfxStateFlags;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

[Flags]
public enum GfxStateFlags : ushort
{
    None = 0,
    DepthTest = 1 << 0,
    DepthWrite = 1 << 1,
    Cull = 1 << 2,
    Blend = 1 << 3,
    Scissor = 1 << 4,
    FramebufferSrgb = 1 << 5,
    ColorMask = 1 << 6,
    PolygonOffset = 1 << 7,
    SampleAlphaCoverage = 1 << 8
}

public readonly struct GfxPassState(GfxStateFlags enabled, GfxStateFlags defined)
{
    public readonly GfxStateFlags Enabled = enabled;
    public readonly GfxStateFlags Defined = defined;

    public bool IsEmpty => Enabled == 0 && Defined == 0;

    public bool IsSet(GfxStateFlags flag) => (Defined & flag) != 0;
    public bool IsEnabled(GfxStateFlags flag) => (Enabled & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxPassState Filter(GfxStateFlags flags) => new(Enabled & flags, Defined & flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState Enable(GfxStateFlags flags) => new(flags, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState Disable(GfxStateFlags flags) => new(0, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState Set(GfxStateFlags enable, GfxStateFlags disable) => new(enable, enable | disable);

    public static GfxPassState Patch(GfxStateFlags defined, GfxStateFlags enabled) => new(enabled, defined);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxStateFlags Merge(GfxStateFlags current, GfxPassState patch)
    {
        var d = patch.Defined;
        return (current & ~d) | (patch.Enabled & d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState PatchWith(GfxPassState baseState, GfxPassState patch)
    {
        var baseEnabled = baseState.Enabled & baseState.Defined;
        var patchEnabled = patch.Enabled & patch.Defined;

        var defined = baseState.Defined | patch.Defined;
        var enabled = Merge(baseEnabled, patch) | patchEnabled;
        return new GfxPassState(enabled, defined);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakeScene() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | FramebufferSrgb | ColorMask | SampleAlphaCoverage,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask |
                     PolygonOffset | SampleAlphaCoverage
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakeSceneEffect() =>
        new(
            enabled: Blend | Cull | FramebufferSrgb | ColorMask | SampleAlphaCoverage,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask |
                     PolygonOffset | SampleAlphaCoverage
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakeShadow() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | FramebufferSrgb | PolygonOffset,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset |
                     SampleAlphaCoverage
        );

    public static GfxPassState MakeLighting() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | Blend | FramebufferSrgb | PolygonOffset,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakePostProcess() =>
        new(
            enabled: FramebufferSrgb | ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakeScreen() =>
        new(
            enabled: ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassState MakeOff() =>
        new(
            enabled: ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );
}