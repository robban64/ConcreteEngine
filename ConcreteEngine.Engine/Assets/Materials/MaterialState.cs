#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Assets.Materials;

public sealed class MaterialState
{
    private bool _clearDirty = false;

    internal bool IsDirty { get; set; }

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

    internal MaterialState(in MaterialImportParams param)
    {
        Color = param.HasColor ? param.Color : Color;
        Shininess = param.HasShininess ? param.Shininess : Shininess;
        Specular = param.HasSpecularFactor ? param.SpecularFactor : Specular;
    }

    public MaterialPipelineState Pipeline
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public Color4 Color
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Color4.White;

    public float Shininess
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 12f;

    public float Specular
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 0.12f;

    public float UvRepeat
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 1f;

    public bool Transparency
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = false;

    internal void Set(in MaterialParamSnapshot param)
    {
        Color = param.Color;
        Specular = param.Specular;
        Shininess = param.Shininess;
        UvRepeat = param.UvRepeat;
        Transparency = param.IsTransparent;
        IsDirty = true;
    }

    internal void ClearDirty()
    {
        if (_clearDirty && IsDirty)
        {
            IsDirty = false;
            _clearDirty = false;
            return;
        }

        _clearDirty = true;
    }
}