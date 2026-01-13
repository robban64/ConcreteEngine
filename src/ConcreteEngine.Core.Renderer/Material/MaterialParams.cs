using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Material;

public struct MaterialParams()
{
    public Color4 Color = Color4.White;
    public float Specular = 0.12f;
    public float Shininess = 12f;
    public float UvRepeat = 1f;
}

public struct MaterialProperties
{
    public bool HasTransparency;
    public bool HasNormal;
    public bool HasAlphaMask;
    public bool HasShadowMap;
}