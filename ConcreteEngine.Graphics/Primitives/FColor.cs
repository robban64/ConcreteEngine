using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Graphics.Primitives;


[StructLayout(LayoutKind.Sequential)]
public readonly struct FColor3
{
    public readonly float R;
    public readonly float G;
    public readonly float B;

    public FColor3(float r, float g, float b)
    {
        R = r; G = g; B = b;
    }
    
    public FColor3(Color4 v)
    {
        R = v.R; G = v.G; B = v.B;
    }

    public static FColor3 ToSrgb(Color4 c) =>
        new(ToSrgb(c.R), ToSrgb(c.G), ToSrgb(c.B));

    public static FColor3 FromSrgb(Color4 c) =>
        new(FromSrgb(c.R), FromSrgb(c.G), FromSrgb(c.B));

    public Color4 ToColor4(float a = 1f) => new(R, G, B, a);

    private static float ToSrgb(float linear) =>
        linear <= 0.0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;

    private static float FromSrgb(float srgb) =>
        srgb <= 0.04045f ? srgb / 12.92f : MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FColor4
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public FColor4(float r, float g, float b, float a = 1f)
    {
        R = r; G = g; B = b; A = a;
    }

    public static FColor4 ToSrgb(Color4 c) =>
        new(ToSrgb(c.R), ToSrgb(c.G), ToSrgb(c.B), c.A);

    public static FColor4 FromSrgb(Color4 c) =>
        new(FromSrgb(c.R), FromSrgb(c.G), FromSrgb(c.B), c.A);

    public Color4 ToColor4() => new(R, G, B, A);

    private static float ToSrgb(float linear) =>
        linear <= 0.0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;

    private static float FromSrgb(float srgb) =>
        srgb <= 0.04045f ? srgb / 12.92f : MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
}