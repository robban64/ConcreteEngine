
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MaterialState
{
    private static int _materialIdCounter;

    public readonly MaterialId MaterialId = new(++_materialIdCounter);
    
    private readonly Material _material;

    public MaterialState(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _material = material;
    }

    internal void SetFromProfile(MaterialProfileEntry profile)
    {
        SetValues(in profile.StateValues);
        DrawState = profile.DrawState;
        DrawFunctions = profile.DrawFunctions;
        
        if (Transparency && profile.DrawQueue == DrawCommandQueue.Opaque)
            DrawQueue = DrawCommandQueue.Transparent;
        else
            DrawQueue = profile.DrawQueue;

        if (profile.Shader.HasShadowSampler) PassMasks |= PassMask.Depth;
        else PassMasks &= ~PassMask.Depth;
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

    public GfxDrawFunctions DrawFunctions
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
    
    
    public void FillValues(out MaterialParams param)
    {
        param.Color = Color;
        param.Shininess = Shininess;
        param.Specular = Specular;
        param.UvRepeat = UvRepeat;
    }

    public void SetValues(in MaterialParams param)
    {
        Color = param.Color;
        Shininess = param.Shininess;
        Specular = param.Specular;
        UvRepeat = param.UvRepeat;
    }

}