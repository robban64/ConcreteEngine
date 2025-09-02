using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Extensions;

namespace ConcreteEngine.Core.Transforms;

public static class Transform
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTransform2D(Vector2 position, Vector2 scale,
        float rotation)
    {
        var translationMat = Matrix4x4.CreateTranslation(position.ToVec3());
        var rotationMat = Matrix4x4.CreateRotationZ(rotation);
        var scaleMat = Matrix4x4.CreateScale(scale.ToVec3(1));

        return scaleMat * rotationMat * translationMat;
    }
}