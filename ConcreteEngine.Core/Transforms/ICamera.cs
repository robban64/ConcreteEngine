using System.Numerics;
using Silk.NET.Maths;

namespace ConcreteEngine.Core;

public interface ICamera
{
    Vector3 Translation { get; }
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
    Matrix4x4 ProjectionViewMatrix { get; }
    Vector2D<int> ViewportSize { get; set; }
}