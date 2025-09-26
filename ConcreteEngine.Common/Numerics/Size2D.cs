using System.Runtime.Serialization;

namespace ConcreteEngine.Common.Numerics;

[DataContract]
public readonly record struct Size2D(
    [property: DataMember(Name = "x")] int Width,
    [property: DataMember(Name = "y")] int Height
)
{
    public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public Size2D ScaleUniform(float factor) => new((int)(Width * factor), (int)(Height * factor));
    public Size2D Scale(float fx, float fy) => new((int)(Width * fx), (int)(Height * fy));
    public (uint Width, uint Height) ToUnsigned() => ((uint)Width, (uint)Height);
    
    public bool IsNegative() =>  Width < 0 || Height < 0;
    public bool IsZero() =>  Width == 0 && Height == 0;
    
}