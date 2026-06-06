
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MaterialState
{
    private static int _materialIdCounter;

    public readonly MaterialId MaterialId = new(++_materialIdCounter);
    
    private readonly TextureSource[] _textureSources;
    
    private readonly Material _material;

    public MaterialState(Material material, TextureSource[] textureSources)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(textureSources);
        _material = material;
        _textureSources = textureSources;
    }

    internal TextureSource[] TextureSources => _textureSources;
    public ReadOnlySpan<TextureSource> GetTextureSources() => _textureSources;
    
    public void SetOverrideTexture(int slot, TextureId textureId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        _textureSources[slot] = _textureSources[slot] with { OverrideTextureId = textureId };
        _material. MarkDirty(AssetDirtyFlag.State);
    }

    public void SetTexture(int slot, Texture? texture)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);

        ref var source = ref _textureSources[slot];

        if (texture != null)
        {
            source = new TextureSource(texture.Id, texture.Usage);
            _material.MarkDirty(AssetDirtyFlag.State);
            return;
        }

        if (source != default)
        {
            source = source with { AssetTexture = AssetId.Empty };
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    }

    public bool Transparency
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.Structure);
        }
    }

    public DrawCommandQueue DrawQueue
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.Structure);
        }
    } = DrawCommandQueue.Opaque;


    public PassMask PassMasks
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.Structure);
        }
    } = PassMask.Default;


    public GfxDrawState DrawState
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.State);
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
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = new(BlendMode.Unset, CullMode.BackCcw, DepthMode.Less, PolygonOffsetLevel.None);


    public Color4 Color
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = Color4.White;

    public float Shininess
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = 12f;

    public float Specular
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = 0.12f;

    public float UvRepeat
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 1f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = 1f;
    
    
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

}