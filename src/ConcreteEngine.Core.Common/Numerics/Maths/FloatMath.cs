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
    public static float Clamp01(float value) => float.Clamp(value, 0f, 1f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp1N1(float v) => float.Max(-1f, float.Min(1f, v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(float degrees) => Deg2Rad * degrees;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(float radians) => Rad2Deg * radians;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(float a, float b, float eps = DefaultEpsilon) => float.Abs(a - b) < eps;
}

public static class DoubleMath
{
    public const double Deg2Rad = Math.PI / 180.0;
    public const double Rad2Deg = 180.0 / Math.PI;

    public const double EpsilonRad = Deg2Rad * 0.01;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp01(double value) => double.Clamp(value, 0.0, 1.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp1N1(double v) => double.Max(-1.0, double.Min(1.0, v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToRadians(double degrees) => Deg2Rad * degrees;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDegrees(double radians) => Rad2Deg * radians;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(double a, double b, double eps = double.Epsilon) => double.Abs(a - b) < eps;
}