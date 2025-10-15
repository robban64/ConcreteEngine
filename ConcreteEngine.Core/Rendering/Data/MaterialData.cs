using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialParams(
    Color4 Color,
    float Specular,
    float Shininess,
    float UvRepeat = 1f,
    // todo remove
    float Normal = 1f);