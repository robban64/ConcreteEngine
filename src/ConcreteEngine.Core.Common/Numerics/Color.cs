using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using static ConcreteEngine.Core.Common.Numerics.Maths.FloatMath;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Color(byte r, byte g, byte b, byte a = 255) : IEquatable<Color>
{
    [JsonInclude] public byte R = r;
    [JsonInclude] public byte G = g;
    [JsonInclude] public byte B = b;
    [JsonInclude] public byte A = a;

    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Transparent = new(0, 0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color4(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color(Color4 c) =>
        new(
            (byte)(Clamp01(c.R) * 255f),
            (byte)(Clamp01(c.G) * 255f),
            (byte)(Clamp01(c.B) * 255f),
            (byte)(Clamp01(c.A) * 255f)
        );

    public (byte R, byte G, byte B) ToByteRgba() => (R, G, B);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint ToPacked() => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color FromPacked(uint packed) =>
        new(
            (byte)packed,
            (byte)(packed >> 8),
            (byte)(packed >> 16),
            (byte)(packed >> 24)
        );

    public override readonly string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Color other) => ToPacked() == other.ToPacked();

    public override readonly bool Equals(object? obj) => obj is Color c && Equals(c);

    public override readonly int GetHashCode() => (int)ToPacked();

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
}