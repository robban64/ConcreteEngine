using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct YawPitch : IEquatable<YawPitch>
{
    public const double PitchLimit = 89.9;

    [JsonInclude] public double Yaw;
    [JsonInclude]  public double Pitch;

    public YawPitch(double yaw, double pitch)
    {
        Yaw = yaw;
        Pitch = pitch;
    }
    
    public YawPitch(float yaw, float pitch)
    {
        Yaw = yaw;
        Pitch = pitch;
    }


    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(YawPitch y) => new((float)y.Yaw, (float)y.Pitch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator YawPitch(Vector2 v) => new(v.X, v.Y);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithClampedPitch() => Pitch = double.Clamp(Pitch, -PitchLimit, PitchLimit);

    //
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
    
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(YawPitch a, YawPitch b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(YawPitch a, YawPitch b) => !a.Equals(b);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(YawPitch other) => Yaw.Equals(other.Yaw) && Pitch.Equals(other.Pitch);

    public override readonly bool Equals(object? obj) => obj is YawPitch other && Equals(other);
    public override readonly int GetHashCode() => Yaw.GetHashCode() * -1521134295 + Pitch.GetHashCode();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YawPitch Lerp(YawPitch a, YawPitch b, double t)
    {
        double yawDelta = b.Yaw - a.Yaw;
        if (yawDelta > 180.0) yawDelta -= 360.0;
        if (yawDelta < -180.0) yawDelta += 360.0;

        YawPitch result;
        result.Yaw = a.Yaw + yawDelta * t;
        result.Pitch = double.Lerp(a.Pitch, b.Pitch, t);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(YawPitch a, YawPitch b, double eps = double.Epsilon) =>
        double.Abs(a.Yaw - b.Yaw) < eps && double.Abs(a.Pitch - b.Pitch) < eps;
    
    

}