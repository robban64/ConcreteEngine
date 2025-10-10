using System.Numerics;

namespace ConcreteEngine.Common.View;

public readonly ref struct ViewProjectionData
{
    public readonly ref Matrix4x4 ViewMatrix;
    public readonly ref Matrix4x4 ProjectionMatrix;
    public readonly ref Matrix4x4 ProjectionViewMatrix;
}
