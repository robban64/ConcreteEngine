#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Transforms;

public sealed class Transform2D
{
    public Transform2D()
    {
    }

    public Transform2D(float x, float y)
    {
        Position = new Vector2(x, y);
    }

    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f; // In radians
    public Vector2 Scale { get; set; } = Vector2.One;

    public Matrix4x4 TransformMatrix => CreateTransformMatrix(Position, Scale, Rotation);
    /*
    {
        get
        {
            var translation = Matrix4X4.CreateTranslation(Position.ToVec3(0));
            var rotation = Matrix4X4.CreateRotationZ(Rotation);
            var scale = Matrix4X4.CreateScale(Scale.ToVec3(1));

            return scale * rotation * translation;
        }
    }
    */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransformMatrix(Vector2 position, Vector2 scale,
        float rotation)
    {
        var translationMat = Matrix4x4.CreateTranslation(position.ToVec3());
        var rotationMat = Matrix4x4.CreateRotationZ(rotation);
        var scaleMat = Matrix4x4.CreateScale(scale.ToVec3(1));

        return scaleMat * rotationMat * translationMat;
    }
}