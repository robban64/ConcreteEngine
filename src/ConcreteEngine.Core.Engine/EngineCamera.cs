using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine;

public abstract class EngineCamera
{
    protected BoundingFrustum Frustum;

    protected Matrix4x4 ViewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 ProjectionMatrix = Matrix4x4.Identity;
    protected Matrix4x4 ProjectionViewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 InvProjectionViewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 RenderViewMatrix = Matrix4x4.Identity;

    public ref readonly BoundingFrustum GetFrustum() => ref Frustum;
    public ref readonly Matrix4x4 GetViewMatrix() => ref ViewMatrix;
    public ref readonly Matrix4x4 GetRenderViewMatrix() => ref RenderViewMatrix;
    public ref readonly Matrix4x4 GetProjectionMatrix() => ref ProjectionMatrix;
    public ref readonly Matrix4x4 GetProjectionViewMatrix() => ref ProjectionViewMatrix;
    public ref readonly Matrix4x4 GetInverseProjectionViewMatrix() => ref InvProjectionViewMatrix;

    public Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public Vector3 Forward => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);

    public abstract Size2D Viewport { get; protected set; }

    public abstract Vector3 Translation { get; set; }
    public abstract YawPitch Orientation { get; set; }
    public abstract float Fov { get; set; }
    public abstract float FarPlane { get; set; }
    public abstract float NearPlane { get; set; }
}