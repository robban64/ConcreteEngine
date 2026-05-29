using System.Runtime.CompilerServices;
using static ConcreteEngine.Graphics.Gfx.GfxStateFlags;

namespace ConcreteEngine.Graphics.Gfx;


public readonly struct GfxPassState(GfxStateFlags enabled, GfxStateFlags defined) : IEquatable<GfxPassState>
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

    public static bool operator ==(GfxPassState left, GfxPassState right) => left.Equals(right);
    public static bool operator !=(GfxPassState left, GfxPassState right) => !left.Equals(right);

    public bool Equals(GfxPassState other) => Enabled == other.Enabled && Defined == other.Defined;

    public override bool Equals(object? obj) => obj is GfxPassState other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Enabled, (int)Defined);

    // UTils
    public static GfxPassState MakeScene() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | Srgb | ColorMask | Ac2,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask |
                     PolygonOffset | Ac2
        );

    public static GfxPassState MakeSceneEffect() =>
        new(
            enabled: Blend | Cull | Srgb | ColorMask | Ac2,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask |
                     PolygonOffset | Ac2
        );

    public static GfxPassState MakeShadow() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | Srgb | PolygonOffset,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask | PolygonOffset |
                     Ac2
        );

    public static GfxPassState MakeLighting() =>
        new(
            enabled: DepthTest | DepthWrite | Cull | Blend | Srgb | PolygonOffset,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask | PolygonOffset
        );

    public static GfxPassState MakePostProcess() =>
        new(
            enabled: ColorMask | Srgb,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask | PolygonOffset
        );

    public static GfxPassState MakeScreen() =>
        new(
            enabled: ColorMask | Srgb,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask | PolygonOffset
        );

    public static GfxPassState MakeOff() =>
        new(
            enabled: ColorMask,
            defined: DepthTest | DepthWrite | Cull | Blend | Scissor | Srgb | ColorMask | PolygonOffset
        );
}