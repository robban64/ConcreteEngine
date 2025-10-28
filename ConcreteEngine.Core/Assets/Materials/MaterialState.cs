#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialState
{
    public Color4 Color { get; set; } = Color4.White;
    public float Shininess { get; set; } = 24f;
    public float Specular { get; set; } = 0.25f;
    public float UvRepeat { get; set; } = 1;
    

    //internal MaterialState(MaterialState param) => Set(param.Snapshot());
    internal MaterialState(MaterialState param)
    {
        Color = param.Color;
        Specular = param.Specular;
        UvRepeat = param.UvRepeat;
        Shininess = param.Shininess;
    }
    internal MaterialState(MaterialDescriptor.MaterialParamsDesc desc)
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

}