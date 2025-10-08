using System.Numerics;

namespace ConcreteEngine.Common.View;

public readonly ref struct ViewProjectionData
{
    public readonly ref Matrix4x4 ViewMatrix;
    public readonly ref Matrix4x4 ProjectionMatrix;

    public ViewProjectionData(ref Matrix4x4 viewMatrix, ref Matrix4x4 projectionMatrix)
    {
        ViewMatrix =  viewMatrix;
        ProjectionMatrix = ref projectionMatrix;
    }
}

public readonly ref struct FullViewProjectionData
{
    public readonly ref Matrix4x4 ViewMatrix;
    public readonly ref Matrix4x4 ProjectionMatrix;
    public readonly ref Matrix4x4 ProjectionViewMatrix;
}

public readonly record struct ViewOrientation(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Up,
    Vector3 Right
);

public readonly record struct ViewProjection(
    float Fov,
    float Aspect,
    float Near,
    float Far
);