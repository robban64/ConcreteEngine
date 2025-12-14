using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;
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

    public GfxPassState Filter(GfxStateFlags flags) => new(Enabled & flags, Defined & flags);

    public static GfxPassState Enable(GfxStateFlags flags) => new(flags, flags);

    public static GfxPassState Disable(GfxStateFlags flags) => new(0, flags);

    public static GfxPassState Set(GfxStateFlags enable, GfxStateFlags disable) => new(enable, enable | disable);

    public static GfxPassState Patch(GfxStateFlags defined, GfxStateFlags enabled) => new(enabled, defined);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxStateFlags Merge(GfxStateFlags current, GfxPassState patch)
    {
        var d = patch.Defined;
        return (current & ~d) | (patch.Enabled & d);
    }

    public static GfxPassState PatchWith(GfxPassState baseState, GfxPassState patch)
    {
        var baseEnabled = baseState.Enabled & baseState.Defined;
        var patchEnabled = patch.Enabled & patch.Defined;

        var defined = baseState.Defined | patch.Defined;
        var enabled = Merge(baseEnabled, patch) | patchEnabled;
        return new GfxPassState(enabled, defined);
    }

    public static GfxPassState MakeScene() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | FramebufferSrgb | ColorMask | SampleAlphaCoverage,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask |
                     PolygonOffset | SampleAlphaCoverage
        );

    public static GfxPassState MakeSceneEffect() =>
        new(
            enabled: Blend | Cull | FramebufferSrgb | ColorMask | SampleAlphaCoverage,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask |
                     PolygonOffset | SampleAlphaCoverage
        );


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

    public static GfxPassState MakePostProcess() =>
        new(
            enabled: FramebufferSrgb | ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );

    public static GfxPassState MakeScreen() =>
        new(
            enabled: ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );

    public static GfxPassState MakeOff() =>
        new(
            enabled: ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | FramebufferSrgb | ColorMask | PolygonOffset
        );
}

public readonly record struct GfxPassStateFunc(
    BlendMode Blend = BlendMode.Unset,
    CullMode Cull = CullMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    PolygonOffsetLevel PolygonOffset = PolygonOffsetLevel.Unset)
{
    public static GfxPassStateFunc MakeDefault() =>
        new(BlendMode.Alpha, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    public static GfxPassStateFunc MakeSky() =>
        new(BlendMode.Unset, CullMode.Unset, DepthMode.Lequal, PolygonOffsetLevel.Unset);

    public static GfxPassStateFunc MakeDepth() =>
        new(BlendMode.Unset, CullMode.FrontCcw, DepthMode.Lequal, PolygonOffsetLevel.Medium);
}

public readonly struct GfxPassClear(in Color4 clearColor, ClearBufferFlag clearBuffer)
{
    public readonly Color4 ClearColor = clearColor;
    public readonly ClearBufferFlag ClearBuffer = clearBuffer;

    public static GfxPassClear MakeColorClear(Color4 clearColor) => new(in clearColor, ClearBufferFlag.Color);

    public static GfxPassClear MakeColorDepthClear(Color4 clearColor) =>
        new(in clearColor, ClearBufferFlag.ColorAndDepth);

    public static GfxPassClear MakeDepthClear() => new(Color4.Black, ClearBufferFlag.Depth);
    public static GfxPassClear MakeNoClear() => new(Color4.Black, ClearBufferFlag.None);
}