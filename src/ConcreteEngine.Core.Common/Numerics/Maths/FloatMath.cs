using System;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class FloatMath
{
    public const float Deg2Rad = MathF.PI / 180f;
    public const float Rad2Deg = 180f / MathF.PI;

    public const float EpsilonRad = Deg2Rad * 0.01f;
    public const float SingularEpsilon = 1e-6f;
    public const float DefaultEpsilon = 1e-5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp1N1(float v) => MathF.Max(-1f, MathF.Min(1f, v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(float degrees) => Deg2Rad * degrees;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(float radians) => Rad2Deg * radians;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(float a, float b, float eps = DefaultEpsilon) => MathF.Abs(a - b) < eps;
}