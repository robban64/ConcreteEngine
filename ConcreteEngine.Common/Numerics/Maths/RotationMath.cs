using System.Numerics;
using System.Runtime.CompilerServices;
using static ConcreteEngine.Common.Numerics.Maths.FloatMath;

namespace ConcreteEngine.Common.Numerics.Maths;

public static class RotationMath
{
    public static YawPitch QuaternionToYawPitch(in Quaternion q)
    {
        const float pitchLimit = 89f;

        var forward = Vector3.Transform(new Vector3(0f, 0f, -1f), q);
        float pitchRad = (float)Math.Asin(Clamp01(forward.Y));
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

    public static Quaternion EulerDegreesToQuaternion(in Vector3 eulerDegrees)
    {
        var rx = eulerDegrees.X * Deg2Rad; // pitch
        var ry = eulerDegrees.Y * Deg2Rad; // yaw
        var rz = eulerDegrees.Z * Deg2Rad; // roll

        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rx);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, ry);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rz);

        var q = Quaternion.Multiply(Quaternion.Multiply(qy, qx), qz);
        return q;
    }

    public static Vector3 QuaternionToEulerDegrees(in Quaternion q, in Vector3 lastEulerDegrees)
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

        // From derived formulas for R = Ry * Rx * Rz:
        // r12 = m12 = -sin(pitch)  => pitch = asin(clamp(-m12))
        // yaw  = atan2(r02, r22) = atan2(m02, m22)
        // roll = atan2(r10, r11) = atan2(m10, m11)
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

        // convert to degrees
        var result = new Vector3(pitchRad * Rad2Deg, yawRad * Rad2Deg, rollRad * Rad2Deg);

        result.X = ClosestAngleDeg(result.X, lastEulerDegrees.X);
        result.Y = ClosestAngleDeg(result.Y, lastEulerDegrees.Y);
        result.Z = ClosestAngleDeg(result.Z, lastEulerDegrees.Z);

        // Normalize into -180,180 for  display
        result.X = NormalizeAngleDeg(result.X);
        result.Y = NormalizeAngleDeg(result.Y);
        result.Z = NormalizeAngleDeg(result.Z);

        return result;
    }

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