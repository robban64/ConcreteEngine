using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplateParams
{
    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float SpecularStrength { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;
    public bool HasNormals { get; set; } = true;
}
