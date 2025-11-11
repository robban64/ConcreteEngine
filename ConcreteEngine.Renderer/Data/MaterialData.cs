#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly record struct MaterialParams
{
    private readonly Color4 _color;

    public Color4 Color
    {
        get => _color;
        init => _color = value;
    }

    public float Specular { get; init; }
    public float Shininess { get; init; }
    public float UvRepeat { get; init; }

    [IgnoreDataMember] public float TransparencyToggle { get; init; }

    [IgnoreDataMember] public float NormalToggle { get; init; }

    [IgnoreDataMember] public float AlphaToggle { get; init; }

    public bool Transparent => TransparencyToggle > 0.1f;
    public bool HasNormal => NormalToggle > 0.1f;
    public bool HasAlpha => AlphaToggle > 0.1f;


    public MaterialParams(
        in Color4 Color,
        float Specular,
        float Shininess,
        float UvRepeat = 1f,
        bool Transparent = false,
        bool HasNormal = false,
        bool HasAlpha = false)
    {
        this.Color = Color;
        this.Specular = Specular;
        this.Shininess = Shininess;
        this.UvRepeat = UvRepeat;
        TransparencyToggle = Transparent ? 1f : 0f;
        NormalToggle = HasNormal ? 1f : 0f;
        AlphaToggle = HasAlpha ? 1f : 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(out Vector4 color, out Vector4 param1, out Vector4 param2)
    {
        color = Color.AsVec4();
        param1 = new Vector4(Specular, UvRepeat, 1.0f, 1.0f);
        param2 = new Vector4(Shininess, NormalToggle, TransparencyToggle, AlphaToggle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTo(ref MaterialUniformRecord record)
    {
        record.MatColor = _color.AsVec4();
        record.MatParams0 = new Vector4(Specular, UvRepeat, 1.0f, 1.0f);
        record.MatParams1 = new Vector4(Shininess, NormalToggle, TransparencyToggle, AlphaToggle);
    }
}

public readonly record struct DrawMaterialMeta(
    MaterialId MaterialId,
    ShaderId ShaderId,
    GfxPassState PassState,
    GfxPassStateFunc PassStateFunc);

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawMaterialPayload(in DrawMaterialMeta meta, in MaterialParams param)
{
    public readonly DrawMaterialMeta Meta = meta;
    public readonly MaterialParams MatParams = param;
}