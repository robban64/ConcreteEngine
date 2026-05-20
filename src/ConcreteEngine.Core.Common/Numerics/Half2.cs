using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Half4(Half x, Half y, Half z, Half w)
{
    public Half4(float x, float y, float z, float w) : this((Half)x, (Half)y, (Half)z, (Half)w) { }

    public Half X = x;
    public Half Y = y;
    public Half Z = z;
    public Half W = w;
}

[StructLayout(LayoutKind.Sequential)]
public struct Half2(Half x, Half y)
{
    public Half X = x;
    public Half Y = y;

    public Half2(float x, float y) : this((Half)x, (Half)y) { }
    public Half2(Vector2 v) : this((Half)v.X, (Half)v.Y) { }

    public float X32
    {
        readonly get => (float)X;
        set => X = (Half)value;
    }

    public float Y32
    {
        readonly get => (float)Y;
        set => Y = (Half)value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Half2 v) => new((float)v.X, (float)v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Half2(Vector2 v) => new(v);

    public override readonly string ToString() => $"({X32}, {Y32})";
}