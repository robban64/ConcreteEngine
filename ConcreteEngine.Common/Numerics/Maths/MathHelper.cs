#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics.Maths;

public sealed class MathHelper
{
    public const float Deg2Rad = MathF.PI / 180f;
    public const float Rad2Deg = 180f / MathF.PI;
    public const float SingularEpsilon = 1e-6f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(float degrees) => Deg2Rad * degrees;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(float radians) => Rad2Deg * radians;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
    {
        return new Vector2(Clamp(value.X, min.X, max.X), Clamp(value.Y, min.Y, max.Y));
    }
}