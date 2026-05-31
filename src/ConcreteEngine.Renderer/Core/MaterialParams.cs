using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Renderer.Core;

public struct MaterialParams(Color4 color, float specular, float shininess, float uvRepeat)
{
    public Color4 Color = color;
    public float Specular = specular;
    public float Shininess = shininess;
    public float UvRepeat = uvRepeat;
}

public struct MaterialRenderProps(bool hasTransparency, bool hasNormal, bool hasAlphaMask, bool hasShadowMap)
{
    public bool HasTransparency = hasTransparency;
    public bool HasNormal = hasNormal;
    public bool HasAlphaMask = hasAlphaMask;
    public bool HasShadowMap = hasShadowMap;
}

public struct MaterialPipeline(GfxDrawState drawState, GfxPassFunctions passFunctions) : IEquatable<MaterialPipeline>
{
    public GfxDrawState DrawState = drawState;
    public GfxPassFunctions PassFunctions = passFunctions;

    public static MaterialPipeline MakeModel(GfxDrawFlags enabled = 0, GfxDrawFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            DrawState = GfxDrawState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull | enabled,
                GfxDrawFlags.Blend | GfxDrawFlags.Ac2 | disabled
            ),
            PassFunctions =
                new GfxPassFunctions(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None)
        };
    }
    /*
    public static MaterialPipeline MakeTransparentModel(GfxDrawFlags enabled = 0, GfxDrawFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            PassState = GfxPassState.Set(
                GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.SampleAlphaCoverage | enabled,
                 GfxDrawFlags.Cull | GfxDrawFlags.Blend | disabled
            ),
            PassFunctions = new GfxPassFunctions(Depth: DepthMode.Lequal)
        };
    }

    public static MaterialPipeline MakeTransparentEffect(GfxDrawFlags enabled = 0, GfxDrawFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            PassState = GfxPassState.Set(
                GfxDrawFlags.DepthTest  | GfxDrawFlags.SampleAlphaCoverage | enabled,
                GfxDrawFlags.DepthWrite |  GfxDrawFlags.Blend | disabled
            ),
            PassFunctions = new GfxPassFunctions(Depth: DepthMode.Lequal)
        };
    }
*/

    public static bool operator ==(MaterialPipeline left, MaterialPipeline right) => left.Equals(right);
    public static bool operator !=(MaterialPipeline left, MaterialPipeline right) => !left.Equals(right);

    public bool Equals(MaterialPipeline other) =>
        DrawState.Equals(other.DrawState) && PassFunctions.Equals(other.PassFunctions);

    public override bool Equals(object? obj) => obj is MaterialPipeline other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(DrawState, PassFunctions);
}