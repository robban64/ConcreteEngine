namespace ConcreteEngine.Core.Common.Numerics;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Color4(float r, float g, float b, float a = 1.0f)
    : IEquatable<Color4>
{
    [JsonInclude] public float R = r;
    [JsonInclude] public float G = g;
    [JsonInclude] public float B = b;
    [JsonInclude] public float A = a;

    //
    public static implicit operator Vector4(Color4 c) => new(c.R, c.G, c.B, c.A);

    public static explicit operator Color4(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    public static explicit operator Color4(Vector3 v) => new(v.X, v.Y, v.Z);

    public readonly Vector3 ToVector3() => new(R, G, B);

    // 
    public readonly uint ToPackedRgba()
    {
        uint r = (uint)(Clamp01(R) * 255.0f + 0.5f);
        uint g = (uint)(Clamp01(G) * 255.0f + 0.5f);
        uint b = (uint)(Clamp01(B) * 255.0f + 0.5f);
        uint a = (uint)(Clamp01(A) * 255.0f + 0.5f);
        return r | (g << 8) | (b << 16) | (a << 24);
    }

    public readonly (byte r, byte g, byte b, byte a) ToBytes()
    {
        return ((byte)(Clamp01(R) * 255f), (byte)(Clamp01(G) * 255f), (byte)(Clamp01(B) * 255f),
            (byte)(Clamp01(A) * 255f));
    }

    public static Color4 FromRgba(byte r, byte g, byte b, byte a = 255)
        => new(r / 255f, g / 255f, b / 255f, a / 255f);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator +(Color4 a, Color4 b) => new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator -(Color4 a, Color4 b) => new(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(Color4 a, float k) => new(a.R * k, a.G * k, a.B * k, a.A * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(float k, Color4 a) => new(a.R * k, a.G * k, a.B * k, a.A * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(Color4 a, Color4 b) => new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

    // 

    public static Color4 Lerp(Color4 a, Color4 b, float t)
    {
        return new Color4(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            a.A + (b.A - a.A) * t
        );
    }

    public readonly Color4 Saturate() => new(Clamp01(R), Clamp01(G), Clamp01(B), Clamp01(A));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

    // 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public readonly bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public override readonly bool Equals(object? obj) => obj is Color4 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public static bool operator ==(Color4 left, Color4 right) => left.Equals(right);
    public static bool operator !=(Color4 left, Color4 right) => !left.Equals(right);
    public override readonly string ToString() => $"[R:{R:F2} G:{G:F2} B:{B:F2} A:{A:F2}]";
    
    // 
    public static Color4 FromHex(ReadOnlySpan<char> hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];

        if (hex.Length == 6)
        {
            return new Color4(
                ParseHex(hex[0..2]) / 255f,
                ParseHex(hex[2..4]) / 255f,
                ParseHex(hex[4..6]) / 255f,
                1.0f
            );
        }
        else if (hex.Length == 8)
        {
            return new Color4(
                ParseHex(hex[0..2]) / 255f,
                ParseHex(hex[2..4]) / 255f,
                ParseHex(hex[4..6]) / 255f,
                ParseHex(hex[6..8]) / 255f
            );
        }

        throw new FormatException($"Invalid hex color length: {hex.Length}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ParseHex(ReadOnlySpan<char> h) => int.Parse(h, System.Globalization.NumberStyles.HexNumber);
    }

    // 

    public static readonly Color4 Transparent = new(0f, 0f, 0f, 0f);
    public static readonly Color4 Black = new(0f, 0f, 0f);
    public static readonly Color4 White = new(1f, 1f, 1f);
    public static readonly Color4 Gray = new(0.5f, 0.5f, 0.5f);
    
    public static readonly Color4 LightGray = FromRgba(192, 192, 192);
    public static readonly Color4 DarkGray = FromRgba(64, 64, 64);

    public static readonly Color4 Red = new(1f, 0f, 0f);
    public static readonly Color4 Green = new(0f, 1f, 0f);
    public static readonly Color4 Blue = new(0f, 0f, 1f);

    public static readonly Color4 Yellow = new(1f, 1f, 0f);
    public static readonly Color4 Magenta = new(1f, 0f, 1f);
    public static readonly Color4 Cyan = new(0f, 1f, 1f);
    public static readonly Color4 Orange = new(1f, 0.647f, 0f);
    public static readonly Color4 CornflowerBlue = new(0.392f, 0.584f, 0.929f);
}