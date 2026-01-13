using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Material;

public struct MaterialParams(Color4 color, float specular, float shininess, float uvRepeat)
{
    public Color4 Color = color;
    public float Specular = specular;
    public float Shininess = shininess;
    public float UvRepeat = uvRepeat;
}

public struct MaterialProperties(bool hasTransparency, bool hasNormal, bool hasAlphaMask, bool hasShadowMap)
{
    public bool HasTransparency = hasTransparency;
    public bool HasNormal = hasNormal;
    public bool HasAlphaMask = hasAlphaMask;
    public bool HasShadowMap = hasShadowMap;
}