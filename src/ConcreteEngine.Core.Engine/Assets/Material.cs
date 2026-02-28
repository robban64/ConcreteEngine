using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Contracts;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Material : AssetObject
{
    public MaterialId MaterialId { get; set; }
    public AssetId TemplateId { get; init; }
    public AssetId AssetShader { get; init; }

    private readonly TextureSource[] _textureSources;

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;


    public Material(string name, AssetId templateId, AssetId assetShader, in MaterialParams param,
        TextureSource[] sources) : base(name)
    {
        ArgumentNullException.ThrowIfNull(sources);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSources = sources;

        SetParams(in param);
        CalculateProperties();
    }

    public Material(string name, AssetId templateId, AssetId assetShader, MaterialParamsRecord param,
        TextureSource[] sources) : base(name)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(param);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSources = sources;

        FromParamRecord(param);
        CalculateProperties();
    }

    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();

    public ReadOnlySpan<TextureSource> GetTextureSources() => _textureSources;
    public MaterialProperties GetProperties() => new(Transparency, HasNormal, HasAlphaMask, HasShadowMap);

    public void SetTexture(int slot, Texture? texture)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _textureSources.Length);


        ref var source = ref _textureSources[slot];

        if (texture is { } tex)
        {
            source = new TextureSource(tex.Id, tex.Usage, tex.TextureKind, tex.PixelFormat);
            MarkDirty();
            return;
        }

        if (source != default)
        {
            source = source with { Texture = AssetId.Empty };
            MarkDirty();
        }
    }

    public void SetPassFunction(GfxPassFunctions passFunctions) =>
        Pipeline = new MaterialPipeline(Pipeline.PassState, passFunctions);

    public void SetPassState(GfxPassState passState) =>
        Pipeline = new MaterialPipeline(passState, Pipeline.PassFunctions);


    public MaterialPipeline Pipeline
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty();
        }
    }

    public Color4 Color
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkDirty();
        }
    } = Color4.White;

    public float Shininess
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            MarkDirty();
        }
    } = 12f;

    public float Specular
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            MarkDirty();
        }
    } = 0.12f;

    public float UvRepeat
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 1f);
            MarkDirty();
        }
    } = 1f;

    public bool Transparency
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty();
        }
    }

    public bool HasAlphaMask
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty();
        }
    }

    public bool HasNormal
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty();
        }
    }

    public bool HasShadowMap
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkDirty();
        }
    }


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

    internal Material MakeNewAsTemplate(AssetId newId, Guid newGId, string newName)
    {
        FillParams(out var param);
        return new Material(newName, Id, AssetShader, in param, _textureSources) { Id = newId, GId = newGId };
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