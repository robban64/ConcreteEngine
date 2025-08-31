using System.Numerics;
using System.Runtime.Serialization;
using Silk.NET.Maths;

namespace ConcreteEngine.Common;

public static class Colors
{
    public static readonly Color4 Transparent = Color4.FromRgba(0, 0, 0, 0);
    public static readonly Color4 Black = Color4.FromRgba(0, 0, 0);
    public static readonly Color4 White = Color4.FromRgba(255, 255, 255);
    public static readonly Color4 Gray = Color4.FromRgba(128, 128, 128);
    public static readonly Color4 LightGray = Color4.FromRgba(192, 192, 192);
    public static readonly Color4 DarkGray = Color4.FromRgba(64, 64, 64);

    public static readonly Color4 Red = Color4.FromRgba(255, 0, 0);
    public static readonly Color4 Green = Color4.FromRgba(0, 255, 0);
    public static readonly Color4 Blue = Color4.FromRgba(0, 0, 255);
    public static readonly Color4 Yellow = Color4.FromRgba(255, 255, 0);
    public static readonly Color4 Magenta = Color4.FromRgba(255, 0, 255);
    public static readonly Color4 Cyan = Color4.FromRgba(0, 255, 255);

    public static readonly Color4 Orange = Color4.FromRgba(255, 165, 0);
    public static readonly Color4 Purple = Color4.FromRgba(128, 0, 128);
    public static readonly Color4 Pink = Color4.FromRgba(255, 105, 180);
    public static readonly Color4 Brown = Color4.FromRgba(139, 69, 19);

    public static readonly Color4 CornflowerBlue = Color4.FromRgba(100, 149, 237);

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

        return Color4.FromRgba((byte)r, (byte)g, (byte)b, (byte)a);

        static int HexVal(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => throw new ArgumentException($"Invalid hex character '{c}'")
        };

        static int ParseHex(ReadOnlySpan<char> span)
            => (HexVal(span[0]) << 4) | HexVal(span[1]);
    }
}

public readonly record struct Color4
{
    private readonly Vector4 _v; // (r,g,b,a), normalized [0..1]

    private Color4(Vector4 normalized) => _v = normalized;

    public static Color4 FromNormalized(float r, float g, float b, float a = 1f)
        => new(new Vector4(ClampNorm(r), ClampNorm(g), ClampNorm(b), ClampNorm(a)));

    public static Color4 FromRgba(byte r, byte g, byte b, byte a = 255)
        => FromNormalized(r / 255f, g / 255f, b / 255f, a / 255f);

    public Vector4 AsVec4() => _v;
    public Vector3 AsVec3() => _v.AsVector3();

    public float R => _v.X;
    public float G => _v.Y;
    public float B => _v.Z;
    public float A => _v.W;

    public Color4 WithAlpha(float a) => FromNormalized(R, G, B, a);
    public Color4 WithAlphaByte(byte a) => FromRgba((byte)(R * 255f), (byte)(G * 255f), (byte)(B * 255f), a);

    public static Color4 Lerp(Color4 from, Color4 to, float t)
        => new Color4(Vector4.Lerp(from._v, to._v, ClampNorm(t)));

    public Color4 Multiply(float scalar)
        => FromNormalized(R * scalar, G * scalar, B * scalar, A);

    public (byte R, byte G, byte B, byte A) ToBytes()
        => ((byte)MathF.Round(R * 255f),
            (byte)MathF.Round(G * 255f),
            (byte)MathF.Round(B * 255f),
            (byte)MathF.Round(A * 255f));

    private static float ClampNorm(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}