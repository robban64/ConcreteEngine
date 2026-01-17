using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

public record struct GfxPassFunctions(
    BlendMode Blend = BlendMode.Unset,
    CullMode Cull = CullMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    PolygonOffsetLevel PolygonOffset = PolygonOffsetLevel.Unset)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassFunctions MakeDefault() =>
        new(BlendMode.Alpha, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassFunctions MakeSky() =>
        new(BlendMode.Unset, CullMode.Unset, DepthMode.Lequal, PolygonOffsetLevel.Unset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxPassFunctions MakeDepth() =>
        new(BlendMode.Unset, CullMode.FrontCcw, DepthMode.Lequal, PolygonOffsetLevel.Medium);
}