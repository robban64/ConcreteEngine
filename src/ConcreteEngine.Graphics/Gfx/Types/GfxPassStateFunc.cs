namespace ConcreteEngine.Graphics.Gfx;

public record struct GfxDrawFunctions(
    BlendMode Blend = BlendMode.Unset,
    CullMode Cull = CullMode.Unset,
    DepthMode Depth = DepthMode.Unset,
    PolygonOffsetLevel PolygonOffset = PolygonOffsetLevel.Unset)
{

    public readonly GfxDrawFunctions Patch(GfxDrawFunctions patch)
    {
        return new GfxDrawFunctions(
            Blend == BlendMode.Unset ? patch.Blend : Blend,
            Cull == CullMode.Unset ? patch.Cull : Cull,
            Depth == DepthMode.Unset ? patch.Depth : Depth,
            PolygonOffset == PolygonOffsetLevel.Unset ? patch.PolygonOffset : PolygonOffset
        );
    }
    public static GfxDrawFunctions MakeDefault() =>
        new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    public static GfxDrawFunctions MakeDepth() =>
        new(BlendMode.Unset, CullMode.FrontCcw, DepthMode.Lequal, PolygonOffsetLevel.Medium);
}