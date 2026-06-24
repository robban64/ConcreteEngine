using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Renderer.Core;

public struct MaterialParams(Color4 color, float specular, float shininess, float uvRepeat = 1f)
{
    public MaterialParams(float specular, float shininess, float uvRepeat = 1f)
        : this(Color4.White, specular, shininess, uvRepeat) { }

    public Color4 Color = color;
    public float Specular = specular;
    public float Shininess = shininess;
    public float UvRepeat = uvRepeat;
}