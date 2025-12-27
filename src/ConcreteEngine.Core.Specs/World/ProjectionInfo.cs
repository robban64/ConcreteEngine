namespace ConcreteEngine.Shared.World;

public struct ProjectionInfo(float fov, float near, float far)
{
    public float AspectRatio;
    public float Fov = fov;
    public float Near = near;
    public float Far = far;
}