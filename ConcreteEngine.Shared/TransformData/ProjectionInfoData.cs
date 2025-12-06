namespace ConcreteEngine.Shared.TransformData;

public struct ProjectionInfoData(float aspectRatio, float fov, float near, float far)
{
    public float AspectRatio = aspectRatio;
    public float Fov = fov;
    public float Near = near;
    public float Far = far;
}