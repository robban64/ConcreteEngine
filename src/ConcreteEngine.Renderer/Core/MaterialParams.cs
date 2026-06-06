using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer.Core;

public struct MaterialParams(Color4 color, float specular, float shininess, float uvRepeat)
{
    public Color4 Color = color;
    public float Specular = specular;
    public float Shininess = shininess;
    public float UvRepeat = uvRepeat;
}

public struct MaterialRenderToggles(bool hasNormal, bool hasAlphaMask, bool hasTransparency)
{
    public bool HasNormal = hasNormal;
    public bool HasAlphaMask = hasAlphaMask;
    public bool HasTransparency = hasTransparency;
}
