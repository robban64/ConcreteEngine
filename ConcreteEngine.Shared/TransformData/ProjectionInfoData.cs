namespace ConcreteEngine.Shared.TransformData;

public readonly struct ProjectionInfoData(float aspectRatio, float fov, float near, float far)
{
    public readonly float AspectRatio = aspectRatio;
    public readonly float Fov = fov;
    public readonly float Near = near;
    public readonly float Far = far;
}