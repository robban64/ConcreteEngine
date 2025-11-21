#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct Color4(float R, float G, float B, float A = 1f)
{
    public static Color4 operator *(Color4 c, float s) => c.Multiply(s);
    public static Color4 operator *(float s, Color4 c) => c.Multiply(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 AsVec4() => new(R, G, B, A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 AsVec3() => new(R, G, B);

    public Color4 Multiply(float scalar) => new(ClampNorm(R * scalar), ClampNorm(G * scalar), ClampNorm(B * scalar), A);

    public Color4 WithAlpha(float a) => this with { A = ClampNorm(a) };
    public Color4 WithAlphaByte(byte a) => this with { A = a / 255f };


    public (byte R, byte G, byte B, byte A) ToBytes() =>
        ((byte)MathF.Round(R * 255f),
            (byte)MathF.Round(G * 255f),
            (byte)MathF.Round(B * 255f),
            (byte)MathF.Round(A * 255f));


    public static Color4 FromNormalized(float r, float g, float b, float a = 1f) =>
        new(ClampNorm(r), ClampNorm(g), ClampNorm(b), ClampNorm(a));

    public static Color4 FromVector4(in Vector4 v) => FromNormalized(v.X, v.Y, v.Z, v.W);
    public static Color4 FromVector3(in Vector3 v, float a = 1f) => FromNormalized(v.X, v.Y, v.Z, a);

    public static Color4 FromRgba(byte r, byte g, byte b, byte a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public static Color4 Lerp(Color4 from, Color4 to, float t)
    {
        t = ClampNorm(t);
        return new Color4(
            from.R + (to.R - from.R) * t,
            from.G + (to.G - from.G) * t,
            from.B + (to.B - from.B) * t,
            from.A + (to.A - from.A) * t
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ClampNorm(float v) => v < 0f ? 0f : v > 1f ? 1f : v;


    public static readonly Color4 Transparent = FromRgba(0, 0, 0, 0);
    public static readonly Color4 Black = FromRgba(0, 0, 0);
    public static readonly Color4 White = FromRgba(255, 255, 255);
    public static readonly Color4 Gray = FromRgba(128, 128, 128);
    public static readonly Color4 LightGray = FromRgba(192, 192, 192);
    public static readonly Color4 DarkGray = FromRgba(64, 64, 64);

    public static readonly Color4 Red = FromRgba(255, 0, 0);
    public static readonly Color4 Green = FromRgba(0, 255, 0);
    public static readonly Color4 Blue = FromRgba(0, 0, 255);
    public static readonly Color4 Yellow = FromRgba(255, 255, 0);
    public static readonly Color4 Magenta = FromRgba(255, 0, 255);
    public static readonly Color4 Cyan = FromRgba(0, 255, 255);

    public static readonly Color4 Orange = FromRgba(255, 165, 0);
    public static readonly Color4 Purple = FromRgba(128, 0, 128);
    public static readonly Color4 Pink = FromRgba(255, 105, 180);
    public static readonly Color4 Brown = FromRgba(139, 69, 19);

    public static readonly Color4 CornflowerBlue = FromRgba(100, 149, 237);

    public static Color4 FromHex(ReadOnlySpan<char> hex)
    {
        if (hex.IsEmpty)
            throw new ArgumentException("hex is empty", nameof(hex));

        if (hex[0] == '#') hex = hex.Slice(1);

        if (hex.Length is not (6 or 8))
            throw new ArgumentException("Use #RRGGBB or #RRGGBBAA.", nameof(hex));

        var r = ParseHex(hex.Slice(0, 2));
        var g = ParseHex(hex.Slice(2, 2));
        var b = ParseHex(hex.Slice(4, 2));
        var a = hex.Length == 8 ? ParseHex(hex.Slice(6, 2)) : 255;

        return FromRgba((byte)r, (byte)g, (byte)b, (byte)a);

        static int HexVal(char c) =>
            c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => throw new ArgumentException($"Invalid hex character '{c}'")
            };

        static int ParseHex(ReadOnlySpan<char> span) => (HexVal(span[0]) << 4) | HexVal(span[1]);
    }
}