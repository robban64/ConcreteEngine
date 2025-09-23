using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Numerics;

public sealed class MathHelper
{
    public const float OneDegree = MathF.PI / 180f;
    public const float OneRadian = 180f / MathF.PI;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(float degrees) => OneDegree * degrees;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(float radians) => OneRadian * radians;

    
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