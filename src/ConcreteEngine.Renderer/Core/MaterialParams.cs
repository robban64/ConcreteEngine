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

public struct MaterialRenderToggles(bool hasNormal, bool hasAlphaMask)
{
    public bool HasNormal = hasNormal;
    public bool HasAlphaMask = hasAlphaMask;
}
