using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

[Flags]
public enum MaterialToggle : byte
{
    None = 0,
    DoubleSided = 1 << 0,
    Transparent = 1 << 1,
    CastShadows = 1 << 2,
    ReceiveShadows = 1 << 3,
    
    Shadows = CastShadows | ReceiveShadows
}


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

    internal void SetFromProfile(MaterialProfile profile)
    {
        Albedo = profile.StateValues.Color;
        SpecularColor = SpecularColor with { A = profile.StateValues.Specular };
        Shininess = profile.StateValues.Shininess;
        UvTransform = UvTransform with { W = profile.StateValues.UvRepeat };
        DrawState = profile.DrawState;
        DrawFunctions = profile.DrawFunctions;

        if (Transparency && profile.DrawQueue == DrawCommandQueue.Opaque)
            DrawQueue = DrawCommandQueue.Transparent;
        else
            DrawQueue = profile.DrawQueue;

        if (profile.Shader.HasShadowSampler) PassMasks |= PassMask.Depth;
        else PassMasks &= ~PassMask.Depth;
    }

    public float Specular
    {
        get => SpecularColor.A;
        set => SpecularColor = SpecularColor with { A = value };
    }

    public float Uv
    {
        get => UvTransform.W;
        set => UvTransform = UvTransform with { W = value };
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


    public Color4 Albedo
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = Color4.White;

    public Color4 SpecularColor
    {
        get;
        set
        {
            if (Color4.NearlyEqual(in field, in value)) return;
            field = value;
            field.A = float.Clamp(value.A, 0f, 1f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = new(1, 1, 1, 0.12f);

    public Vector4 UvTransform
    {
        get;
        set
        {
            if (VectorMath.NearlyEqual(field, value)) return;
            field = value;
            field.W = float.Max(value.W, 1f);

            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = new (0, 0, 1f, 1f);

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

    public float Roughness
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    }

    public float Metallic
    {
        get;
        set
        {
            if (FloatMath.NearlyEqual(field, value)) return;
            field = float.Max(value, 0f);
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    }
}