#region

using System.Numerics;
using System.Runtime.Serialization;

#endregion

namespace ConcreteEngine.Common.Numerics;

[DataContract]
public readonly record struct Size2D(
    [property: DataMember(Name = "width")] int Width,
    [property: DataMember(Name = "height")]
    int Height
)
{
    public Size2D(int size) : this(size, size)
    {
    }

    public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public Size2D ScaleUniform(float factor) => new((int)(Width * factor), (int)(Height * factor));
    public Size2D Scale(float fx, float fy) => new((int)(Width * fx), (int)(Height * fy));
    public Size2D Scale(Vector2 v) => new((int)(Width * v.X), (int)(Height * v.Y));

    public (uint Width, uint Height) ToUnsigned() => ((uint)Width, (uint)Height);

    public Bounds2D ToBounds2D() => new(0, 0, Width, Height);
    public Vector2I ToVector2I() => new(Width, Height);

    public static Size2D FromVector2I(Vector2I v) => new(v.X, v.Y);


    public bool IsNegative() => Width < 0 || Height < 0;
    public bool IsZero() => Width == 0 && Height == 0;

    public static Size2D Zero => new(0, 0);
    public static Size2D One => new(1, 1);
}