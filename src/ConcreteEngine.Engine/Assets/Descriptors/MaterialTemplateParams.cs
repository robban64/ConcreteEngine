using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Material;

namespace ConcreteEngine.Engine.Assets.Descriptors;

public sealed class MaterialParamsRecord
{
    public Color4? Color { get; init; }
    public float? Shininess { get; init; }
    public float? Specular { get; init; }
    public float? UvRepeat { get; init; }
}