using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Core.Engine.Graphics;


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