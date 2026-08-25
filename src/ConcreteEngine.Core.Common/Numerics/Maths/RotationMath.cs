using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class RotationMath
{
    public static YawPitch QuaternionToYawPitch(Quaternion q)
    {
        const float pitchLimit = 89f;

        var forward = Vector3.Transform(new Vector3(0f, 0f, -1f), q);
        float pitchRad = float.Asin(FloatMath.Clamp1N1(forward.Y));
        float yawRad = float.Atan2(forward.X, forward.Z);

        float yawDeg = yawRad * FloatMath.Rad2Deg;
        float pitchDeg = pitchRad * FloatMath.Rad2Deg;

        if (pitchDeg > pitchLimit) pitchDeg = pitchLimit;
        else if (pitchDeg < -pitchLimit) pitchDeg = -pitchLimit;

        return new YawPitch(yawDeg, pitchDeg);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion YawPitchToQuaternion(YawPitch orientation)
    {
        double yaw = orientation.Yaw * DoubleMath.Deg2Rad;
        double pitch = orientation.Pitch * DoubleMath.Deg2Rad;

        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)yaw);
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)pitch);

        return Quaternion.Multiply(qy, qx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion EulerDegreesToQuaternion(Vector3 eulerDegrees)
    {
        return Quaternion.CreateFromYawPitchRoll(
            eulerDegrees.Y * FloatMath.Deg2Rad,
            eulerDegrees.X * FloatMath.Deg2Rad,
            eulerDegrees.Z * FloatMath.Deg2Rad
        );
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

        float m22 = 1f - 2f * (xx + yy);

        float pitchRad = float.Asin(float.Clamp(-m12, -1f, 1f));
        float cosPitch = float.Cos(pitchRad);

        float yawRad, rollRad;

        if (float.Abs(cosPitch) > FloatMath.SingularEpsilon)
        {
            yawRad = float.Atan2(m02, m22);
            rollRad = float.Atan2(m10, m11);
        }
        else
        {
            // Gimbal singularity
            if (m12 <= -1f + FloatMath.DefaultEpsilon) // pitch near +90 deg
            {
                pitchRad = MathF.PI / 2.0f;
                yawRad = float.Atan2(m01, m00);
                rollRad = 0f;
            }
            else // m12 >= +1 -> pitch near -90 deg
            {
                pitchRad = -MathF.PI / 2.0f;
                yawRad = float.Atan2(-m01, m00);
                rollRad = 0f;
            }
        }

        return new Vector3(
            NormalizeAngleDeg(pitchRad * FloatMath.Rad2Deg),
            NormalizeAngleDeg(yawRad * FloatMath.Rad2Deg),
            NormalizeAngleDeg(rollRad * FloatMath.Rad2Deg)
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