using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct GfxDrawState(GfxDrawFlags Enabled, GfxDrawFlags Defined)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Enabled == 0 && Defined == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(GfxDrawFlags flag) => (Defined & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(GfxDrawFlags flag) => (Enabled & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxDrawState WithEnabled(GfxDrawFlags flag) => new(Enabled | flag, Defined | flag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxDrawState WithDisabled(GfxDrawFlags flag) => new(Enabled & ~flag, Defined | flag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Enable(GfxDrawFlags flags) => new(flags, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Disable(GfxDrawFlags flags) => new(0, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxDrawState Set(GfxDrawFlags enable, GfxDrawFlags disable) => new(enable, enable | disable);
}