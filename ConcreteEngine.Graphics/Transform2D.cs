#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics;

public sealed class Transform2D
{
    public Transform2D()
    {
    }

    public Transform2D(float x, float y)
    {
        Position = new Vector2D<float>(x, y);
    }

    public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
    public float Rotation { get; set; } = 0f; // In radians
    public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;

    public Matrix4X4<float> TransformMatrix => CreateTransformMatrix(Position, Scale, Rotation);
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
    public static Matrix4X4<float> CreateTransformMatrix(Vector2D<float> position, Vector2D<float> scale,
        float rotation)
    {
        var translationMat = Matrix4X4.CreateTranslation(position.ToVec3(0));
        var rotationMat = Matrix4X4.CreateRotationZ(rotation);
        var scaleMat = Matrix4X4.CreateScale(scale.ToVec3(1));

        return scaleMat * rotationMat * translationMat;
    }
}