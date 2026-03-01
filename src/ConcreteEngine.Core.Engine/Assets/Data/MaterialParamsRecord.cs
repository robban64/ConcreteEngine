using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Assets.Data;

public sealed class MaterialParamsRecord
{
    public Color4? Color { get; init; }
    public float? Shininess { get; init; }
    public float? Specular { get; init; }
    public float? UvRepeat { get; init; }
}