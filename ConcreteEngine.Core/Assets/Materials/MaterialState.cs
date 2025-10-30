#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialState
{
    private Color4 _color = Color4.White;
    private float _shininess = 12f;
    private float _specular = 0.12f;
    private float _uvRepeat = 1f;
    private bool _dirty;
    private bool _clearDirty = false;


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


    public bool Dirty => _dirty;

    internal void ClearDirty()
    {
        if (_clearDirty && _dirty)
        {
            _dirty = false;
            _clearDirty = false;
            return;
        }

        _clearDirty = true;
    }

    public Color4 Color
    {
        get => _color;
        set
        {
            _color = value;
            _dirty = true;
        }
    }

    public float Shininess
    {
        get => _shininess;
        set
        {
            _shininess = value;
            _dirty = true;
        }
    }

    public float Specular
    {
        get => _specular;
        set
        {
            _specular = value;
            _dirty = true;
        }
    }

    public float UvRepeat
    {
        get => _uvRepeat;
        set
        {
            _uvRepeat = value;
            _dirty = true;
        }
    }

    internal void Set(in MaterialParams param)
    {
        Color = param.Color;
        Specular = param.Specular;
        Shininess = param.Shininess;
        UvRepeat = param.UvRepeat;
    }
}