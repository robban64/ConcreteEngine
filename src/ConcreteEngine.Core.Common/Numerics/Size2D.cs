using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Size2D(int Width, int Height)
{
    public Size2D(int size) : this(size, size) { }
    public Size2D(float x, float y) : this((int)x, (int)y) { }

    [JsonIgnore]
    public float AspectRatio
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Height == 0 ? 0f : (float)Width / Height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Size2D v) => new(v.Width, v.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Size2D(Vector2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Size2D(Size3D v) => new(v.Width, v.Height);


    public Size2D ScaleUniform(float factor) => new((int)(Width * factor), (int)(Height * factor));
    public Size2D Scale(float fx, float fy) => new((int)(Width * fx), (int)(Height * fy));
    public Size2D Scale(Vector2 v) => new((int)(Width * v.X), (int)(Height * v.Y));

    public (uint Width, uint Height) ToUnsigned() => ((uint)Width, (uint)Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2I ToVector2I() => new(Width, Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToVector2() => new(Width, Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size3D ToSize3D(int depth) => Size3D.From(this, depth);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size2D Clamp(Size2D min, Size2D max) =>
        new(int.Clamp(Width, min.Width, max.Width), int.Clamp(Height, min.Height, max.Height));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNegative() => Width < 0 || Height < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsZero() => Width == 0 && Height == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNegativeOrZero() => IsNegative() || IsZero();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Size2D a, Size2D b) => a.Width > b.Width && a.Height > b.Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Size2D a, Size2D b) => a.Width < b.Width && a.Height < b.Height;


    public static Size2D Zero => new(0, 0);
    public static Size2D One => new(1, 1);

    public override string ToString()
    {
        return $"{Width}x{Height}";
    }
}