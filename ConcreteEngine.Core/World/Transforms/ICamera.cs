#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Core.World;

public interface ICamera
{
    Vector3 Translation { get; }
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
    Matrix4x4 ProjectionViewMatrix { get; }
    Size2D Viewport { get; set; }
}