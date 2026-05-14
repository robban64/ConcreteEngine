using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct ColorRgba(byte r, byte g, byte b, byte a = 255) : IEquatable<ColorRgba>
{
    [JsonInclude] public byte R = r;
    [JsonInclude] public byte G = g;
    [JsonInclude] public byte B = b;
    [JsonInclude] public byte A = a;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color4(ColorRgba c) => Color4.FromRgba(c.R, c.G, c.B, c.A);

    public readonly (byte R, byte G, byte B) ToByteRgba() => (R, G, B);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint ToPacked() => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorRgba FromPacked(uint packed) =>
        new((byte)packed, (byte)(packed >> 8), (byte)(packed >> 16), (byte)(packed >> 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorRgba Lerp(ColorRgba a, ColorRgba b, byte t)
    {
        ColorRgba result;

        result.R = (byte)(a.R + (((b.R - a.R) * t) >> 8));
        result.G = (byte)(a.G + (((b.G - a.G) * t) >> 8));
        result.B = (byte)(a.B + (((b.B - a.B) * t) >> 8));
        result.A = (byte)(a.A + (((b.A - a.A) * t) >> 8));

        return result;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ColorRgba left, ColorRgba right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ColorRgba left, ColorRgba right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(ColorRgba other) => ToPacked() == other.ToPacked();

    public readonly override bool Equals(object? obj) => obj is ColorRgba c && Equals(c);

    public readonly override int GetHashCode() => (int)ToPacked();
    public readonly override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";


    public static readonly ColorRgba White = new(255, 255, 255);
    public static readonly ColorRgba Black = new(0, 0, 0);
    public static readonly ColorRgba Transparent = new(0, 0, 0, 0);
}