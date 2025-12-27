using System.Runtime.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[DataContract]
public readonly record struct Size3D(
    [property: DataMember(Name = "width")] int Width,
    [property: DataMember(Name = "height")] int Height,
    [property: DataMember(Name = "depth")] int Depth
)
{
    public float AspectRatioXy => Height == 0 ? 0f : (float)Width / Height;
    public float AspectRatioXz => Depth == 0 ? 0f : (float)Width / Depth;
    public float AspectRatioYz => Depth == 0 ? 0f : (float)Height / Depth;

    public Size3D ScaleUniform(float factor) =>
        new((int)(Width * factor), (int)(Height * factor), (int)(Depth * factor));

    public Size3D Scale(float fx, float fy, float fz) => new((int)(Width * fx), (int)(Height * fy), (int)(Depth * fz));

    public (uint Width, uint Height, uint Depth) ToUnsigned() => ((uint)Width, (uint)Height, (uint)Depth);

    public bool IsNegative() => Width < 0 || Height < 0 || Depth < 0;
    public bool IsZero() => Width == 0 && Height == 0 && Depth == 0;


    public static Size3D From(Size2D size, int depth) => new(size.Width, size.Height, depth);
}