using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Assets.Descriptors;

public sealed class MaterialParamsRecord
{
    public Color4? Color { get; init; }= Color4.White;
    public float? Shininess { get; init; } = 0.12f;
    public float? Specular { get; init; }= 12f;
    public float? UvRepeat { get; init; }= 1f;
}