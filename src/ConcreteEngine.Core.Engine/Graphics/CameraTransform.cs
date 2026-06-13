using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class CameraFrustum
{
    private BoundingFrustum _frustum;

    public ref readonly BoundingFrustum GetBoundingFrustum() => ref _frustum;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(in Matrix4x4 viewProj) => _frustum.UpdateFrom(in viewProj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsBox(in BoundingBox box)
    {
        for (int i = 0; i < 6; i++)
        {
            if (CollisionMethods.IsOutsidePlane(in box, in Unsafe.Add(ref _frustum.LeftPlane, i))) return false;
        }

        return true;
    }
}

public sealed class CameraTransformSnapshot
{
    public Vector3 Translation;
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;

    public Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public Vector3 Forward => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);
}

public sealed class CameraTransform
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 InverseProjectionViewMatrix;

    public Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public Vector3 Forward => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);

    public Vector2 Tan => new(1f / ProjectionMatrix.M11, 1f / ProjectionMatrix.M22);
}