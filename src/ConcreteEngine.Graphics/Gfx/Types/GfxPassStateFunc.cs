namespace ConcreteEngine.Graphics.Gfx;

public record struct GfxDrawFunctions(
    BlendMode Blend = BlendMode.Unset,
    CullMode Cull = CullMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    PolygonOffsetLevel PolygonOffset = PolygonOffsetLevel.Unset)
{
    public static GfxDrawFunctions MakeDefault() =>
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    public static GfxDrawFunctions MakeDepth() =>
        new(BlendMode.Unset, CullMode.FrontCcw, DepthMode.Lequal, PolygonOffsetLevel.Medium);
}