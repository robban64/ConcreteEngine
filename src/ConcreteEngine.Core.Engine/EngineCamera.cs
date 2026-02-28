using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine;

public abstract class EngineCamera
{
    protected BoundingFrustum _frustum;

    protected Matrix4x4 _viewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
    protected Matrix4x4 _projectionViewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 _invProjectionViewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 _renderViewMatrix = Matrix4x4.Identity;

    public ref readonly BoundingFrustum GetFrustum() => ref _frustum;
    public ref readonly Matrix4x4 GetViewMatrix() => ref _viewMatrix;
    public ref readonly Matrix4x4 GetProjectionMatrix() => ref _projectionMatrix;
    public ref readonly Matrix4x4 GetProjectionViewMatrix() => ref _projectionViewMatrix;
    public ref readonly Matrix4x4 GetInverseProjectionViewMatrix() => ref _invProjectionViewMatrix;

    public Vector3 Right => new(_viewMatrix.M11, _viewMatrix.M21, _viewMatrix.M31);
    public Vector3 Up => new(_viewMatrix.M12, _viewMatrix.M22, _viewMatrix.M32);
    public Vector3 Forward => new(-_viewMatrix.M13, -_viewMatrix.M23, -_viewMatrix.M33);

    public abstract Size2D Viewport { get; protected set; }

    public abstract Vector3 Translation { get; set; }
    public abstract YawPitch Orientation { get; set; }
    public abstract float Fov { get; set; }
    public abstract float FarPlane { get; set; }
    public abstract float NearPlane { get; set; }
}