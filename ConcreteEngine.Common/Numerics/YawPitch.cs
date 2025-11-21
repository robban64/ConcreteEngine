#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct YawPitch(float Yaw, float Pitch)
{
    public const float PitchLimit = 89.9f;

    public YawPitch WithClampedPitch() => this with { Pitch = float.Clamp(Pitch, -PitchLimit, PitchLimit) };

    public Vector2 AsVec2() => new(Yaw, Pitch);
    public (float, float) AsTuple() => (Yaw, Pitch);
    
    public YawPitch AddYaw(float yaw) => this with { Yaw = Yaw + yaw }; 
    public YawPitch AddPitch(float pitch) => this with { Pitch = Pitch + pitch }; 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator +(YawPitch a, YawPitch b) => new(a.Yaw + b.Yaw, a.Pitch + b.Pitch);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator +(YawPitch a, float b) => new(a.Yaw + b, a.Pitch + b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator -(YawPitch a, YawPitch b) => new(a.Yaw - b.Yaw, a.Pitch - b.Pitch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator -(YawPitch v) => new(-v.Yaw, -v.Pitch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator *(YawPitch v, float k) => new(v.Yaw * k, v.Pitch * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch operator *(float k, YawPitch v) => new(v.Yaw * k, v.Pitch * k);

    

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