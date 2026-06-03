using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Material : AssetObject
{
    private static int _materialIdCounter = 0;
    public static Material FallbackMaterial { get; internal set; } = null!;

    public AssetId TemplateId { get; init; }
    public MaterialId MaterialId { get; private set; } = new(++_materialIdCounter);
    public MaterialProfile Profile { get; private set; }
    public MaterialRenderToggles RenderToggles { get; private set; }

    public Shader? BoundShader { get; internal set; }

    public readonly MaterialState State;

    private readonly TextureSource[] _textureSources;

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    private Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader, MaterialProfile profile,
        TextureSource[] sources) : base(name,id,gid)
    {
        ArgumentNullException.ThrowIfNull(sources);

        TemplateId = templateId;
        BoundShader = boundShader;
        _textureSources = sources;
        Profile = profile;
        State = new MaterialState(this);

        CalculateProperties();
        MarkDirty();
    }

    public Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader, MaterialProfile profile,
        in MaterialParams param,
        TextureSource[] sources) : this(name,id,gid, templateId, boundShader, profile, sources)
    {
        State.SetParams(in param);
    }

    public Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader, MaterialProfile profile,
        MaterialParamsRecord param, TextureSource[] sources) : this(name,id,gid, templateId, boundShader, profile, sources)
    {
        ArgumentNullException.ThrowIfNull(param);

        FromParamRecord(param);
    }


    public bool HasTransparency => State.Transparency;
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

        if (texture != null)
        {
            source = new TextureSource(texture.Id, texture.Usage, texture.GpuState.TextureKind, texture.GpuState.PixelFormat);
            MarkDirty();
            return;
        }

        if (source != default)
        {
            source = source with { AssetTexture = AssetId.Empty };
            MarkDirty();
        }
    }


    internal Material MakeNewAsTemplate(AssetId newId, Guid newGId, string newName)
    {
        State.FillParams(out var param);
        return new Material(newName, newId, newGId, Id, BoundShader, Profile, in param, _textureSources);
    }

    internal void Commit()
    {
        CalculateProperties();
    }

    private void CalculateProperties()
    {
        var props = new MaterialRenderToggles
        {
            HasTransparency = HasTransparency, HasShadowMap = BoundShader?.DefaultBindings.ShadowMapBinding >= 0
        };
        foreach (var source in _textureSources)
        {
            if (!source.AssetTexture.IsValid()) continue;
            if (!props.HasNormal) props.HasNormal = source.Usage == TextureUsage.Normal;
            if (!props.HasAlphaMask) props.HasAlphaMask = source.Usage == TextureUsage.Mask;
        }

        RenderToggles = props;
    }

    private void FromParamRecord(MaterialParamsRecord param)
    {
        if (param.Color is { } color) State.Color = color;
        if (param.Shininess is { } shininess) State.Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) State.UvRepeat = uvRepeat;
        if (param.Specular is { } spec) State.Specular = spec;
    }
}


public sealed class MaterialState(Material material)
{
    private void MarkDirty() => material.MarkDirty();
    
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

    public GfxDrawState DrawState
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkDirty();
        }
    } = GfxDrawState.Set(
        GfxDrawFlags.DepthTest | GfxDrawFlags.DepthWrite | GfxDrawFlags.Cull,
        GfxDrawFlags.Blend | GfxDrawFlags.Ac2
    );

    public GfxPassFunctions PassFunctions
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkDirty();
        }
    } = new (BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);

    

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
}
