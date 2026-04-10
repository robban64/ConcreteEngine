using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Data;

public struct ViewTransform(in Vector3 translation, YawPitch orientation)
{
    public Vector3 Translation = translation;
    public YawPitch Orientation = orientation;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ViewTransform Lerp(in ViewTransform a, in ViewTransform b, float alpha)
    {
        return new ViewTransform(
            Vector3.Lerp(a.Translation, b.Translation, alpha),
            YawPitch.LerpFixed(a.Orientation, b.Orientation, alpha)
        );
    }
}