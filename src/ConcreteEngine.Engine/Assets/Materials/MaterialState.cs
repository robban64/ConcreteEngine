using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Assets.Materials;

public sealed class MaterialState
{
    internal static class DirtyState
    {
        public static readonly HashSet<MaterialId> DirtyIds = new(16);
    }

    public MaterialId Id { get; internal set; }

    private bool _clearDirty;

    internal bool IsDirty
    {
        get => DirtyState.DirtyIds.Contains(Id);
        private set
        {
            if (Id == 0) return;
            if (value) DirtyState.DirtyIds.Add(Id);
            else DirtyState.DirtyIds.Remove(Id);
        }
    }


    internal MaterialState(MaterialState param)
    {
        Color = param.Color;
        Specular = param.Specular;
        UvRepeat = param.UvRepeat;
        Shininess = param.Shininess;
    }

    internal MaterialState(MaterialRecord.MaterialParamsDesc desc)
    {
        Color = desc.Color ?? Color;
        Shininess = desc.Shininess ?? Shininess;
        Specular = desc.Specular ?? Specular;
        UvRepeat = desc.UvRepeat ?? UvRepeat;
    }

    internal MaterialState(in MaterialImportData data,  MaterialImportProps props)
    {
        Color = props.HasColor ? data.Color : Color;
        Shininess = props.HasShininess ? data.Shininess : Shininess;
        Specular = props.HasSpecularFactor ? data.SpecularFactor : Specular;
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

    internal void Set(in RenderMaterial param)
    {
        Color = param.Color;
        Specular = param.Specular;
        Shininess = param.Shininess;
        UvRepeat = param.UvRepeat;
        Transparency = param.HasTransparency;
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