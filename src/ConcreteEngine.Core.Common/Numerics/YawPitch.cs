using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public record struct YawPitch(float Yaw, float Pitch)
{
    public const float PitchLimit = 89.9f;

    public float Yaw = Yaw;
    public float Pitch = Pitch;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(YawPitch y) => new(y.Yaw, y.Pitch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator YawPitch(Vector2 v) => new(v.X, v.Y);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithClampedPitch() => Pitch = float.Clamp(Pitch, -PitchLimit, PitchLimit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 AsVec2() => new(Yaw, Pitch);

    public static YawPitch operator +(YawPitch a, YawPitch b) => new(a.Yaw + b.Yaw, a.Pitch + b.Pitch);

    public static YawPitch operator +(YawPitch a, float b) => new(a.Yaw + b, a.Pitch + b);

    public static YawPitch operator -(YawPitch a, YawPitch b) => new(a.Yaw - b.Yaw, a.Pitch - b.Pitch);

    public static YawPitch operator -(YawPitch v) => new(-v.Yaw, -v.Pitch);

    public static YawPitch operator *(YawPitch v, float k) => new(v.Yaw * k, v.Pitch * k);

    public static YawPitch operator *(float k, YawPitch v) => new(v.Yaw * k, v.Pitch * k);


    public static YawPitch FromVector2(Vector2 vector) => new(vector.X, vector.Y);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch Lerp(YawPitch a, YawPitch b, float dt) =>
        new(float.Lerp(a.Yaw, b.Yaw, dt), float.Lerp(a.Pitch, b.Pitch, dt));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch LerpFixed(YawPitch a, YawPitch b, float t)
    {
        float yawDelta = b.Yaw - a.Yaw;
        if (yawDelta > 180f) yawDelta -= 360f;
        if (yawDelta < -180f) yawDelta += 360f;

        float yaw = a.Yaw + yawDelta * t;
        float pitch = float.Lerp(a.Pitch, b.Pitch, t);

        return new YawPitch(yaw, pitch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(YawPitch a, YawPitch b, float eps = FloatMath.EpsilonRad) =>
        MathF.Abs(a.Yaw - b.Yaw) < eps && MathF.Abs(a.Pitch - b.Pitch) < eps;

    public readonly void Deconstruct(out float Yaw, out float Pitch)
    {
        Yaw = this.Yaw;
        Pitch = this.Pitch;
    }
}