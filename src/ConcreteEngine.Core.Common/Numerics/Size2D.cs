using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Size2D(int Width, int Height)
{
    public Size2D(int size) : this(size, size)
    {
    }

    [JsonIgnore] public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public Size2D ScaleUniform(float factor) => new((int)(Width * factor), (int)(Height * factor));
    public Size2D Scale(float fx, float fy) => new((int)(Width * fx), (int)(Height * fy));
    public Size2D Scale(Vector2 v) => new((int)(Width * v.X), (int)(Height * v.Y));

    public (uint Width, uint Height) ToUnsigned() => ((uint)Width, (uint)Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bounds2D ToBounds2D() => new(0, 0, Width, Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2I ToVector2I() => new(Width, Height);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToVector2() => new(Width, Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size2D Clamp(Size2D min, Size2D max) =>
        new(int.Clamp(Width, min.Width, max.Width), int.Clamp(Height, min.Height, max.Height));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNegative() => Width < 0 || Height < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsZero() => Width == 0 && Height == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNegativeOrZero() => IsNegative() || IsZero();

    public static Size2D Zero => new(0, 0);
    public static Size2D One => new(1, 1);
}