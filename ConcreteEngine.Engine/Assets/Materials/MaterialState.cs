#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Assets.Materials;

public sealed class MaterialState
{
    private bool _clearDirty = false;

    private Color4 _color = Color4.White;
    private float _shininess = 12f;
    private float _specular = 0.12f;
    private float _uvRepeat = 1f;

    private bool _transparency = false;
    private MaterialPipelineState _pipeline;


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

    public MaterialPipelineState Pipeline
    {
        get => _pipeline;
        set
        {
            _pipeline = value;
            IsDirty = true;
        }
    }

    public Color4 Color
    {
        get => _color;
        set
        {
            _color = value;
            IsDirty = true;
        }
    }

    public float Shininess
    {
        get => _shininess;
        set
        {
            _shininess = value;
            IsDirty = true;
        }
    }

    public float Specular
    {
        get => _specular;
        set
        {
            _specular = value;
            IsDirty = true;
        }
    }

    public float UvRepeat
    {
        get => _uvRepeat;
        set
        {
            _uvRepeat = value;
            IsDirty = true;
        }
    }

    public bool Transparency
    {
        get => _transparency;
        set
        {
            _transparency = value;
            IsDirty = true;
        }
    }

    internal void Set(in MaterialParams param)
    {
        Color = param.Color;
        Specular = param.Specular;
        Shininess = param.Shininess;
        UvRepeat = param.UvRepeat;
        Transparency = param.Transparent;
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