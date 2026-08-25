using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Color4(float r, float g, float b, float a = 1.0f) : IEquatable<Color4>
{
    [JsonInclude] public float R = r;
    [JsonInclude] public float G = g;
    [JsonInclude] public float B = b;
    [JsonInclude] public float A = a;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(in Color4 c) => Unsafe.BitCast<Color4, Vector4>(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(in Color4 c) => new(c.R, c.G, c.B);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color4(in Vector4 v) => Unsafe.BitCast<Vector4, Color4>(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color4(Vector3 v) => new(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ColorRgba(in Color4 c) => c.ToRgba();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator +(Color4 a, Color4 b) => new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator +(Color4 a, float k) => new(a.R + k, a.G + k, a.B + k, a.A + k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator -(Color4 a, Color4 b) => new(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator -(Color4 a, float k) => new(a.R - k, a.G - k, a.B - k, a.A - k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(Color4 a, float k) => new(a.R * k, a.G * k, a.B * k, a.A * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(float k, Color4 a) => new(a.R * k, a.G * k, a.B * k, a.A * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 operator *(Color4 a, Color4 b) => new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Color4 left, Color4 right) => left.AsVector128() == right.AsVector128();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Color4 left, Color4 right) => !(left == right);
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Color4 other) => this.AsVector128() == other.AsVector128();
    public override readonly bool Equals(object? obj) => obj is Color4 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override readonly string ToString() => $"[R:{R:F2} G:{G:F2} B:{B:F2} A:{A:F2}]";

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clamp()
    {
        R = float.Clamp(R, 0.0f, 1.0f);
        G = float.Clamp(G, 0.0f, 1.0f);
        B = float.Clamp(G, 0.0f, 1.0f);
        A = float.Clamp(A, 0.0f, 1.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Color4 WithClampedAlpha() => this with { A = float.Clamp(A, 0f, 1f) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ColorRgba ToRgba()
    {
        ColorRgba result;
        result.R = (byte)(R * 255f);
        result.G = (byte)(G * 255f);
        result.B = (byte)(B * 255f);
        result.A = (byte)(A * 255f);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint ToPackedRgba()
    {
        uint r = (uint)(R * 255.0f + 0.5f);
        uint g = (uint)(G * 255.0f + 0.5f);
        uint b = (uint)(B * 255.0f + 0.5f);
        uint a = (uint)(A * 255.0f + 0.5f);
        return r | (g << 8) | (b << 16) | (a << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 FromRgba(byte r, byte g, byte b, byte a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(Color4 a, Color4 b, float eps = FloatMath.DefaultEpsilon) =>
        VectorMath.NearlyEqual(a.AsVector128(), b.AsVector128(), eps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 Lerp(Color4 a, Color4 b, float t) =>
        Vector128.Lerp(a.AsVector128(), b.AsVector128(), Vector128.Create(t)).AsColor4();


    // 
    public static Color4 FromHex(ReadOnlySpan<char> hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];

        if (hex.Length == 6)
        {
            return new Color4(
                ParseHex(hex[..2]) / 255f,
                ParseHex(hex[2..4]) / 255f,
                ParseHex(hex[4..6]) / 255f,
                1.0f
            );
        }

        if (hex.Length == 8)
        {
            return new Color4(
                ParseHex(hex[..2]) / 255f,
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

    public static Color4 Transparent => new(0f, 0f, 0f, 0f);
    public static Color4 Black => new(0f, 0f, 0f);
    public static Color4 White => new(1f, 1f, 1f);
    public static Color4 Gray => new(0.5f, 0.5f, 0.5f);

    public static Color4 Red => new(1f, 0f, 0f);
    public static Color4 Green => new(0f, 1f, 0f);
    public static Color4 Blue => new(0f, 0f, 1f);

    public static Color4 Yellow => new(1f, 1f, 0f);
    public static Color4 Magenta => new(1f, 0f, 1f);
    public static Color4 Cyan => new(0f, 1f, 1f);
    public static Color4 Orange => new(1f, 0.647f, 0f);
    public static Color4 CornflowerBlue => new(0.392f, 0.584f, 0.929f);
}