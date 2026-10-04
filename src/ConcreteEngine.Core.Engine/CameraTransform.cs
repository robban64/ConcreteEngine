using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class CameraTransformSnapshot
{
    public Vector3 Translation;
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 ProjectionViewMatrix;
    
    public void From(Vector3 translation, Vector2 orientation, in Matrix4x4 projectionMatrix)
    {
        var quaternion = RotationMath.YawPitchToQuaternion(orientation);
        MatrixMath.CreateFixedSizeModelMatrix(translation, in quaternion, out var viewMatrix);
        Matrix4x4.Invert(viewMatrix, out viewMatrix);
        
        Translation = translation;
        ViewMatrix = viewMatrix;
        ProjectionMatrix = projectionMatrix;
        ProjectionViewMatrix = viewMatrix * projectionMatrix;
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
    
    public Vector2 Tan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(1f / ProjectionMatrix.M11, 1f / ProjectionMatrix.M22);
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