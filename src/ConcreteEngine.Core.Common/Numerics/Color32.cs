using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using static ConcreteEngine.Core.Common.Numerics.Maths.FloatMath;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Color32(byte r, byte g, byte b, byte a = 255) : IEquatable<Color32>
{
    [JsonInclude] public byte R = r;
    [JsonInclude] public byte G = g;
    [JsonInclude] public byte B = b;
    [JsonInclude] public byte A = a;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color4(Color32 c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Color32(Color4 c) =>
        new(
            (byte)(Clamp01(c.R) * 255f),
            (byte)(Clamp01(c.G) * 255f),
            (byte)(Clamp01(c.B) * 255f),
            (byte)(Clamp01(c.A) * 255f)
        );

    public readonly (byte R, byte G, byte B) ToByteRgba() => (R, G, B);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint ToPacked() => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color32 FromPacked(uint packed) =>
        new((byte)packed, (byte)(packed >> 8), (byte)(packed >> 16), (byte)(packed >> 24));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Color32 left, Color32 right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Color32 left, Color32 right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Color32 other) => ToPacked() == other.ToPacked();

    public override readonly bool Equals(object? obj) => obj is Color32 c && Equals(c);

    public override readonly int GetHashCode() => (int)ToPacked();
    public override readonly string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";


    public static readonly Color32 White = new(255, 255, 255);
    public static readonly Color32 Black = new(0, 0, 0);
    public static readonly Color32 Transparent = new(0, 0, 0, 0);
}