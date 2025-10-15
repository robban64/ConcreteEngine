using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialId(int Id)
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);
}

public readonly record struct MaterialParams(
    Color4 Color,
    float Specular,
    float Shininess,
    float UvRepeat = 1f,
    // todo remove
    float Normal = 1f);