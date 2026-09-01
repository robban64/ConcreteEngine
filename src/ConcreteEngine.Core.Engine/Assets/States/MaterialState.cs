using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[Flags]
public enum MaterialShading : byte
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

    public readonly Id16<Material> MaterialId = new(++_materialIdCounter);

    private readonly Material _material;
    private MaterialShading _shading;

    public MaterialState(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _material = material;
    }

    public bool CastShadow => (_shading & MaterialShading.CastShadows) != 0;
    public bool ReceiveShadows => (_shading & MaterialShading.ReceiveShadows) != 0;
    public bool IsTransparent => (_shading & MaterialShading.Transparent) != 0;

    public bool HasAlphaMask { get; internal set; }

    internal void SetFromProfile(MaterialProfile profile)
    {
        profile.StateValues.WriteTo(this);

        DrawState = profile.DrawState;
        DrawFunctions = profile.DrawFunctions;
        _shading = profile.Shading;

        if (CastShadow) Passes |= PassMask.Depth;
        else Passes &= ~PassMask.Depth;

        if (IsTransparent && profile.DrawQueue == DrawQueue.Opaque)
            DrawQueue = DrawQueue.Transparent;
        else
            DrawQueue = profile.DrawQueue;
    }

    public float Specular
    {
        get => SpecularColor.A;
        set => SpecularColor = SpecularColor with { A = value };
    }

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

    public DrawQueue DrawQueue
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.Structure);
        }
    } = DrawQueue.Opaque;


    public PassMask Passes
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.Structure);
        }
    } = PassMask.Default;

    [InputColor]
    public Color4 Color
    {
        get;
        set
        {
            var color = value.WithClampedAlpha();
            if (Color4.NearlyEqual(field, color)) return;
            field = color;
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = Color4.White;

    [InputColor]
    public Color4 SpecularColor
    {
        get;
        set
        {
            var color = value.WithClampedAlpha();
            if (Color4.NearlyEqual(field, color)) return;
            field = color;
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    } = new(1, 1, 1, 0.12f);

    [InputNumber]
    public Vector2 UvOffset
    {
        get;
        set
        {
            if (VectorMath.NearlyEqual(field, value)) return;
            field = value;
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    }

    [InputNumber]
    public Vector2 UvRepeat
    {
        get;
        set
        {
            if (VectorMath.NearlyEqual(field, value)) return;
            field = new Vector2(float.Max(value.X, 1f), float.Max(value.Y, 1f));
            _material.MarkDirty(AssetDirtyFlag.State);
        }
    }

    [InputNumber(InputStyle.Slider, Min = 0, Max = 50f)]
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

    [InputNumber(InputStyle.Slider, Min = 0, Max = 50f)]
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

    [InputNumber(InputStyle.Slider, Min = 0, Max = 50f)]
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