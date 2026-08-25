using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class CameraFrustum
{
    private BoundingFrustum _mainFrustum;
    private BoundingFrustum _lightFrustum;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateMain(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out _mainFrustum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLight(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out _lightFrustum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PassMask Intersects(PassMask passes, in BoundingAxisBox box)
    {
        var center = new Vector4(box.Center, 1f);
        var extent = new Vector4(box.Extent, 0f);

        var mask = PassMask.None;
        if ((passes & PassMask.Main) != 0 && IntersectsMain(center, extent)) mask |= PassMask.Main;
        if ((passes & PassMask.Depth) != 0 && IntersectsLight(center, extent)) mask |= PassMask.Depth;
        return mask;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IntersectsMain(Vector4 center4, Vector4 extent4)
    {
        ref var planes = ref Unsafe.As<BoundingFrustum, Vector4>(ref _mainFrustum);
        ref readonly var end = ref Unsafe.Add(ref planes, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref planes, in end))
        {
            if (CollisionMethods.IsOutsidePlane(center4, extent4, in planes)) return false;
            planes = ref Unsafe.Add(ref planes, 1);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IntersectsLight(Vector4 center4, Vector4 extent4)
    {
        ref var planes = ref Unsafe.As<BoundingFrustum, Vector4>(ref _lightFrustum);
        ref readonly var end = ref Unsafe.Add(ref planes, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref planes, in end))
        {
            if (CollisionMethods.IsOutsidePlane(center4, extent4, in planes)) return false;
            planes = ref Unsafe.Add(ref planes, 1);
        }

        return true;
    }

}

public sealed class CameraTransformSnapshot
{
    public Vector3 Translation;
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 ProjectionViewMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateViewMatrix(Vector3 translation, YawPitch orientation)
    {
        Translation = translation;

        ref var viewMatrix = ref ViewMatrix;
        var quaternion = RotationMath.YawPitchToQuaternion(orientation);
        MatrixMath.CreateFixedSizeModelMatrix(in translation, in quaternion, out viewMatrix);
        Matrix4x4.Invert(viewMatrix, out viewMatrix);
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