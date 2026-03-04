using System.Numerics;
using System.Runtime.CompilerServices;
using static ConcreteEngine.Core.Common.Numerics.Maths.FloatMath;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class RotationMath
{
    public static YawPitch QuaternionToYawPitch(in Quaternion q)
    {
        const float pitchLimit = 89f;

        var forward = Vector3.Transform(new Vector3(0f, 0f, -1f), q);
        float pitchRad = (float)Math.Asin(Clamp1N1(forward.Y));
        float yawRad = (float)Math.Atan2(forward.X, forward.Z);

        float yawDeg = yawRad * Rad2Deg;
        float pitchDeg = pitchRad * Rad2Deg;

        if (pitchDeg > pitchLimit) pitchDeg = pitchLimit;
        else if (pitchDeg < -pitchLimit) pitchDeg = -pitchLimit;

        return new YawPitch(yawDeg, pitchDeg);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion YawPitchToQuaternion(YawPitch orientation)
    {
        float yaw = orientation.Yaw * Deg2Rad;
        float pitch = orientation.Pitch * Deg2Rad;

        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);

        return Quaternion.Multiply(qy, qx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion EulerDegreesToQuaternion(in Vector3 eulerDegrees)
    {
        return Quaternion.CreateFromYawPitchRoll(
            eulerDegrees.Y * Deg2Rad, 
            eulerDegrees.X * Deg2Rad,  
            eulerDegrees.Z * Deg2Rad 
        );
        /*
        var rx = eulerDegrees.X * Deg2Rad; // pitch
        var ry = eulerDegrees.Y * Deg2Rad; // yaw
        var rz = eulerDegrees.Z * Deg2Rad; // roll

        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rx);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, ry);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rz);

        var q = Quaternion.Multiply(Quaternion.Multiply(qy, qx), qz);
        return q;
        */
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 QuaternionToEulerDegrees(in Quaternion q)
    {
        // convert quaternion -> rotation matrix
        float x = q.X, y = q.Y, z = q.Z, w = q.W;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;

        float m00 = 1f - 2f * (yy + zz);
        float m01 = 2f * (xy - wz);
        float m02 = 2f * (xz + wy);

        float m10 = 2f * (xy + wz);
        float m11 = 1f - 2f * (xx + zz);
        float m12 = 2f * (yz - wx);

        float m20 = 2f * (xz - wy);
        float m21 = 2f * (yz + wx);
        float m22 = 1f - 2f * (xx + yy);
        
        float pitchRad = (float)Math.Asin(Math.Clamp(-m12, -1f, 1f));
        float cosPitch = (float)Math.Cos(pitchRad);

        float yawRad, rollRad;

        if (Math.Abs(cosPitch) > SingularEpsilon)
        {
            yawRad = (float)Math.Atan2(m02, m22);
            rollRad = (float)Math.Atan2(m10, m11);
        }
        else
        {
            // Gimbal singularity
            if (m12 <= -1f + 1e-5f) // pitch near +90 deg
            {
                pitchRad = (float)(Math.PI / 2.0);
                yawRad = (float)Math.Atan2(m01, m00);
                rollRad = 0f;
            }
            else // m12 >= +1 -> pitch near -90 deg
            {
                pitchRad = (float)(-Math.PI / 2.0);
                yawRad = (float)Math.Atan2(-m01, m00);
                rollRad = 0f;
            }
        }


        return new Vector3(
            NormalizeAngleDeg(pitchRad * Rad2Deg),
            NormalizeAngleDeg(yawRad   * Rad2Deg),
            NormalizeAngleDeg(rollRad  * Rad2Deg)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float NormalizeAngleDeg(float a)
    {
        a %= 360f;
        if (a <= -180f) a += 360f;
        else if (a > 180f) a -= 360f;
        return a;
    }

    private static float ClosestAngleDeg(float angle, float reference)
    {
        angle %= 360f;
        reference %= 360f;

        float diff = reference - angle;
        float shift = (float)Math.Round(diff / 360f);
        angle += shift * 360f;

        float d = angle - reference;
        if (d > 180f) angle -= 360f;
        else if (d <= -180f) angle += 360f;
        return angle;
    }
}