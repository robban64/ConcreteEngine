using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class CameraFrustum
{
    private BoundingFrustum _frustum;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(in Matrix4x4 viewProj) => _frustum.UpdateFrom(in viewProj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsBox(in BoundingBox box)
    {
        var bounds = box;
        ref var start = ref Unsafe.As<BoundingFrustum, Plane>(ref _frustum);
        for (int i = 0; i < 6; ++i)
        {
            if (CollisionMethods.IsOutsidePlane(in bounds, in Unsafe.Add(ref start, i))) return false;
        }

        return true;
    }
}

public sealed class CameraTransformSnapshot
{
    public Vector3 Translation;
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateViewMatrix( Vector3 translation, YawPitch orientation)
    {
        Translation = translation;
        
        ref var viewMatrix = ref ViewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(in translation,
            RotationMath.YawPitchToQuaternion(orientation),
            out viewMatrix);

        Matrix4x4.Invert(viewMatrix, out viewMatrix);
    }

    public Matrix4x4 ProjectionViewMatrix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ViewMatrix * ProjectionMatrix;
    }

    public Vector3 Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    }

    public Vector3 Up
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    }

    public Vector3 Forward
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);
    }
}

public sealed class CameraTransform
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 InverseProjectionViewMatrix;

    public Vector3 Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    }

    public Vector3 Up
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    }

    public Vector3 Forward
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(-ViewMatrix.M13, -ViewMatrix.M23, -ViewMatrix.M33);
    }

    public Vector2 Tan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(1f / ProjectionMatrix.M11, 1f / ProjectionMatrix.M22);
    }
}