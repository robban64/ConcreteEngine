using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Rendering.Data;

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplateParams
{
    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float Specular { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;

    internal MaterialTemplateParams(MaterialTemplateParams param) => Set(param.GetDataParams());

    internal MaterialTemplateParams(MaterialDescriptor.MaterialParamsDesc desc)
    {
        Color = desc.Color ?? Color;
        Shininess = desc.Shininess ?? Shininess;
        Specular = desc.Specular ?? Specular;
        UvRepeat = desc.UvRepeat ?? UvRepeat;
    }

    internal void Set(in MaterialParams param)
    {
        Color = param.Color;
        Specular = param.Specular;
        Shininess = param.Shininess;
        UvRepeat = param.UvRepeat;
    }

    public MaterialParams GetDataParams() => new(
        Color: Color,
        Specular: Specular,
        Shininess: Shininess,
        UvRepeat: UvRepeat
    );
}