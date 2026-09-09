using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class CameraFrustum
{
    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    private Span<Vector4> LightPlanes => _frustumPlanes.AsSpan(0, 6);
    private Span<Vector4> ScenePlanes => _frustumPlanes.AsSpan(6);

    private ref BoundingFrustum LightFrustum
        => ref Unsafe.As<Vector4, BoundingFrustum>(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes));

    private ref BoundingFrustum MainFrustum => ref Unsafe.As<Vector4, BoundingFrustum>
        (ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateMain(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out MainFrustum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLight(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);
    }

    public PassMask Intersects(PassMask passes, in BoundingAxisBox box)
    {
        var center = new Vector4(box.Center, 1f);
        var extent = new Vector4(box.Extent, 0f);

        var mask = PassMask.None;
        if ((passes & PassMask.Depth) != 0)
        {
            var test = TestIntersect(LightPlanes, in center, in extent);
            mask |= test ? PassMask.Depth : 0;
        }

        if ((passes & PassMask.Main) != 0)
        {
            var test = TestIntersect(ScenePlanes, in center, in extent);
            mask |= test ? PassMask.Main : 0;
        }

        return mask;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestIntersect(Span<Vector4> span, in Vector4 center4, in Vector4 extent4)
    {
        ref var plane = ref MemoryMarshal.GetReference(span);
        ref readonly var end = ref Unsafe.Add(ref plane, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref plane, in end))
        {
            bool isOutside = CollisionMethods.IsOutsidePlane(center4, extent4, in plane);
            if (isOutside) return false;
            plane = ref Unsafe.Add(ref plane, 1);
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
    public void UpdateViewMatrix(in Vector3D translation, Vector2D orientation)
    {
        var translationF = Translation = (Vector3)translation;
        var quaternion = RotationMath.YawPitchToQuaternion(orientation);

        ref var viewMatrix = ref ViewMatrix;
        MatrixMath.CreateFixedSizeModelMatrix(translationF, in quaternion, out viewMatrix);
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