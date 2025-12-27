using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Visuals;

namespace ConcreteEngine.Engine.Worlds.Utility;

internal static class CameraUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetWorldBounds(in BoundingBox local, in Transform transform, out BoundingBox world)
    {
        var worldCenter = Vector3.Transform(local.Center, transform.Rotation) + transform.Translation;
        var localExtent = local.Extent;

        var rotMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation);

        var m11 = MathF.Abs(rotMatrix.M11);
        var m12 = MathF.Abs(rotMatrix.M12);
        var m13 = MathF.Abs(rotMatrix.M13);
        var m21 = MathF.Abs(rotMatrix.M21);
        var m22 = MathF.Abs(rotMatrix.M22);
        var m23 = MathF.Abs(rotMatrix.M23);
        var m31 = MathF.Abs(rotMatrix.M31);
        var m32 = MathF.Abs(rotMatrix.M32);
        var m33 = MathF.Abs(rotMatrix.M33);

        float wEx = (localExtent.X * m11 + localExtent.Y * m21 + localExtent.Z * m31) * transform.Scale.X;
        float wEy = (localExtent.X * m12 + localExtent.Y * m22 + localExtent.Z * m32) * transform.Scale.Y;
        float wEz = (localExtent.X * m13 + localExtent.Y * m23 + localExtent.Z * m33) * transform.Scale.Z;

        // Reconstruct World AABB
        world.Min.X = worldCenter.X - wEx;
        world.Min.Y = worldCenter.Y - wEy;
        world.Min.Z = worldCenter.Z - wEz;

        world.Max.X = worldCenter.X + wEx;
        world.Max.Y = worldCenter.Y + wEy;
        world.Max.Z = worldCenter.Z + wEz;
    }

    public static void CreateLightView(
        ref LightView view,
        in ShadowParams shadowParams,
        Vector3 lightDirection,
        Span<Vector3> corners
    )
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var center = FrustumMath.GetFrustumCenter(corners);
        float farthestDistSqr = 0f;
        foreach (var c in corners)
        {
            float d = Vector3.DistanceSquared(center, c);
            if (d > farthestDistSqr) farthestDistSqr = d;
        }

        float radius = MathF.Sqrt(farthestDistSqr);
        float diameter = radius * 2.0f;

        var shadowRotation = Matrix4x4.CreateLookAt(default, -dir, worldUp);
        Vector3 centerLs = Vector3.Transform(center, shadowRotation);

        float texelSize = diameter / (float)shadowParams.ShadowMapSize;
        float snappedX = MathF.Floor(centerLs.X / texelSize) * texelSize;
        float snappedY = MathF.Floor(centerLs.Y / texelSize) * texelSize;

        Vector3 snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);

        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);
        Vector3 snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - dir * shadowParams.Distance * 0.5f;
        view.LightViewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            float z = Vector3.Transform(corners[i], view.LightViewMatrix).Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        float nearLs = -maxZ - shadowParams.ZPad;
        float farLs = -minZ + shadowParams.ZPad;

        view.LightProjectionMatrix = Matrix4x4.CreateOrthographic(
            diameter,
            diameter,
            nearLs,
            farLs
        );

        view.LightSpaceMatrix = view.LightViewMatrix * view.LightProjectionMatrix;
    }
}