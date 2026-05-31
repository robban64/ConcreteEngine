using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct GfxDrawState(GfxDrawFlags Enabled, GfxDrawFlags Defined)
{
    public readonly GfxDrawFlags Enabled = Enabled;
    public readonly GfxDrawFlags Defined = Defined;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Enabled == 0 && Defined == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(GfxDrawFlags flag) => (Defined & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(GfxDrawFlags flag) => (Enabled & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxDrawState Filter(GfxDrawFlags flags) => new(Enabled & flags, Defined & flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Enable(GfxDrawFlags flags) => new(flags, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Disable(GfxDrawFlags flags) => new(0, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Set(GfxDrawFlags enable, GfxDrawFlags disable) => new(enable, enable | disable);
}