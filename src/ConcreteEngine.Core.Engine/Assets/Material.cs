using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Material : AssetObject
{
    public AssetId TemplateId { get; init; }
    public AssetId ShaderId { get; internal set; }
    public MaterialId MaterialId { get; internal set; }
    
    public MaterialProfile Profile { get; internal set; }
    public MaterialRenderProps RenderProps { get; private set; }

    private readonly TextureSource[] _textureSources;

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    private Material(string name, AssetId templateId, AssetId shaderId, MaterialProfile profile,
        TextureSource[] sources) : base(name)
    {
        ArgumentNullException.ThrowIfNull(sources);

        TemplateId = templateId;
        ShaderId = shaderId;
        _textureSources = sources;
        Profile = profile;

        CalculateProperties();
    }

    public Material(string name, AssetId templateId, AssetId shaderId, MaterialProfile profile, in MaterialParams param,
        TextureSource[] sources) : this(name, templateId, shaderId, profile, sources)
    {
        SetParams(in param);
    }

    public Material(string name, AssetId templateId, AssetId shaderId, MaterialProfile profile,
        MaterialParamsRecord param, TextureSource[] sources) : this(name, templateId, shaderId, profile, sources)
    {
        ArgumentNullException.ThrowIfNull(param);

        FromParamRecord(param);
    }


    public ReadOnlySpan<TextureSource> GetTextureSources() => _textureSources;

    public void SetOverrideTexture(int slot, TextureId textureId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        _textureSources[slot] = _textureSources[slot] with { OverrideTextureId = textureId };
    }

    public void SetTexture(int slot, Texture? texture)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);

        ref var source = ref _textureSources[slot];

        if (texture is { } tex)
        {
            source = new TextureSource(tex.Id, tex.Usage, tex.TextureKind, tex.PixelFormat);
            MarkDirty();
            return;
        }

        if (source != default)
        {
            source = source with { AssetTexture = AssetId.Empty };
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
    } = MaterialPipeline.MakeModel();

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
        return new Material(newName, Id, ShaderId, Profile, in param, _textureSources) { Id = newId, GId = newGId };
    }

    internal void Commit()
    {
        CalculateProperties();
    }

    private void CalculateProperties()
    {
        var props = new MaterialRenderProps { HasTransparency = Transparency };
        foreach (var source in _textureSources)
        {
            if (!props.HasShadowMap) props.HasShadowMap = source.Usage == TextureUsage.Shadowmap;
            if (!source.AssetTexture.IsValid()) continue;
            if (!props.HasNormal) props.HasNormal = source.Usage == TextureUsage.Normal;
            if (!props.HasAlphaMask) props.HasAlphaMask = source.Usage == TextureUsage.Mask;
        }

        RenderProps = props;
    }

    private void FromParamRecord(MaterialParamsRecord param)
    {
        if (param.Color is { } color) Color = color;
        if (param.Shininess is { } shininess) Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) UvRepeat = uvRepeat;
        if (param.Specular is { } spec) Specular = spec;
    }
}