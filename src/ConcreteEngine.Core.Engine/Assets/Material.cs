using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Material : AssetObject
{
    public static class DirtyState
    {
        public static readonly HashSet<MaterialId> DirtyIds = new(16);
    }

    public MaterialId MaterialId { get; set; }
    public AssetId TemplateId { get; init; }
    public AssetId AssetShader { get; init; }

    private readonly TextureSource[] _textureSources;

    private bool _clearDirty;

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    public override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();

    public Material(AssetId templateId, AssetId assetShader, in MaterialParams param, TextureSource[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSources = sources;

        SetParams(in param);
        CalculateProperties();
    }

    public Material(AssetId templateId, AssetId assetShader, MaterialParamsRecord param, TextureSource[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(param);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSources = sources;

        FromParamRecord(param);
        CalculateProperties();
    }

    public ReadOnlySpan<TextureSource> GetTextureSources() => _textureSources;

    public void SetTexture(int slot, Texture texture)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _textureSources.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(texture.GfxId.Value);
        _textureSources[slot] = new TextureSource(texture.Id, texture.Usage, texture.TextureKind, texture.PixelFormat);
        IsDirty = true;
    }

    public MaterialPipeline Pipeline
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
            field = float.Max(value, 0f);
            IsDirty = true;
        }
    } = 12f;

    public float Specular
    {
        get;
        set
        {
            field = float.Max(value, 0f);
            IsDirty = true;
        }
    } = 0.12f;

    public float UvRepeat
    {
        get;
        set
        {
            field = float.Max(value, 1f);
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
    }

    public bool HasAlphaMask
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool HasNormal
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool HasShadowMap
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public void ClearDirty()
    {
        if (_clearDirty && IsDirty)
        {
            IsDirty = false;
            _clearDirty = false;
            return;
        }

        _clearDirty = true;
    }

    public bool IsDirty
    {
        get => DirtyState.DirtyIds.Contains(MaterialId);
        private set
        {
            if (Id == 0) return;
            if (value) DirtyState.DirtyIds.Add(MaterialId);
            else DirtyState.DirtyIds.Remove(MaterialId);
        }
    }


    public Material MakeNewAsTemplate(AssetId newId, Guid newGId, string newName)
    {
        FillParams(out var param);
        return new Material(Id, AssetShader, in param, _textureSources) { Id = newId, GId = newGId, Name = newName };
    }

    public MaterialProperties GetProperties() => new(Transparency, HasNormal, HasAlphaMask, HasShadowMap);

    public void FillParams(out MaterialParams param)
    {
        param.Color = Color;
        param.Shininess = Shininess;
        param.Specular = Specular;
        param.UvRepeat = UvRepeat;
    }

    public void SetParams(in MaterialParams param)
    {
        Color = param.Color;
        Shininess = param.Shininess;
        Specular = param.Specular;
        UvRepeat = param.UvRepeat;
    }

    private void CalculateProperties()
    {
        foreach (var slot in _textureSources)
        {
            if (!HasShadowMap) HasShadowMap = slot.Usage == TextureUsage.Shadowmap;
            if (!slot.Texture.IsValid()) continue;
            if (!HasNormal) HasNormal = slot.Usage == TextureUsage.Normal;
            if (!HasAlphaMask) HasAlphaMask = slot.Usage == TextureUsage.Mask;
        }
    }

    private void FromParamRecord(MaterialParamsRecord param)
    {
        if (param.Color is { } color) Color = color;
        if (param.Shininess is { } shininess) Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) UvRepeat = uvRepeat;
        if (param.Specular is { } spec) Specular = spec;
    }
}