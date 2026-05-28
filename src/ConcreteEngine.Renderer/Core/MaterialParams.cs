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

public struct MaterialPipeline(GfxPassState passState, GfxPassFunctions passFunctions) : IEquatable<MaterialPipeline>
{
    public GfxPassState PassState = passState;
    public GfxPassFunctions PassFunctions = passFunctions;

    public static MaterialPipeline MakeModel(GfxStateFlags enabled = 0, GfxStateFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            PassState = GfxPassState.Set(
                GfxStateFlags.DepthTest | GfxStateFlags.DepthWrite | GfxStateFlags.Cull | enabled,
                 GfxStateFlags.Blend | GfxStateFlags.SampleAlphaCoverage | disabled
            ),
            PassFunctions = new GfxPassFunctions(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None)
        };
    }
    /*
    public static MaterialPipeline MakeTransparentModel(GfxStateFlags enabled = 0, GfxStateFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            PassState = GfxPassState.Set(
                GfxStateFlags.DepthTest | GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage | enabled,
                 GfxStateFlags.Cull | GfxStateFlags.Blend | disabled
            ),
            PassFunctions = new GfxPassFunctions(Depth: DepthMode.Lequal)
        };
    }
    
    public static MaterialPipeline MakeTransparentEffect(GfxStateFlags enabled = 0, GfxStateFlags disabled = 0)
    {
        return new MaterialPipeline
        {
            PassState = GfxPassState.Set(
                GfxStateFlags.DepthTest  | GfxStateFlags.SampleAlphaCoverage | enabled,
                GfxStateFlags.DepthWrite |  GfxStateFlags.Blend | disabled
            ),
            PassFunctions = new GfxPassFunctions(Depth: DepthMode.Lequal)
        };
    }
*/

    public static bool operator ==(MaterialPipeline left, MaterialPipeline right) => left.Equals(right);
    public static bool operator !=(MaterialPipeline left, MaterialPipeline right) => !left.Equals(right);

    public bool Equals(MaterialPipeline other) =>
        PassState.Equals(other.PassState) && PassFunctions.Equals(other.PassFunctions);

    public override bool Equals(object? obj) => obj is MaterialPipeline other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(PassState, PassFunctions);
}