using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplateParams
{
    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float Specular { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;
    public bool HasNormals { get; set; } = true;

    public MaterialParams DataParams => new(
        Color: Color,
        Specular: Specular,
        Shininess: Shininess,
        UvRepeat: UvRepeat
    );
}