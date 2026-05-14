namespace ConcreteEngine.Core.Common.Visuals;

public struct ProjectionInfo(float fov, float near, float far)
{
    public float Fov = fov;
    public float Near = near;
    public float Far = far;
}