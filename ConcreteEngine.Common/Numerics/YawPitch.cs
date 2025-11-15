#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct YawPitch(float Yaw, float Pitch)
{
    public const float PitchLimit = 89.9f;

    public Vector2 AsVec2() => new(Yaw, Pitch);

    public (float, float) AsTuple() => (Yaw, Pitch);

    public YawPitch WithClampedPitch() => this with { Pitch = float.Clamp(Pitch, -PitchLimit, PitchLimit) };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToQuaternion(out Quaternion quaternion) => RotationMath.YawPitchToQuaternion(this, out quaternion);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch FromQuaternion(in Quaternion quaternion) => RotationMath.QuaternionToYawPitch(in quaternion);

    public static YawPitch FromVector2(Vector2 vector) => new(vector.X, vector.Y);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch Lerp(YawPitch a, YawPitch b, float dt) =>
        new(float.Lerp(a.Yaw, b.Yaw, dt), float.Lerp(a.Pitch, b.Pitch, dt));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(YawPitch a, YawPitch b, float eps = FloatMath.EpsilonRad) =>
        MathF.Abs(a.Yaw - b.Yaw) < eps && MathF.Abs(a.Pitch - b.Pitch) < eps;
}