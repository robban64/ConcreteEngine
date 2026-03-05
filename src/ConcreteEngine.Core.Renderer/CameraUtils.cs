using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Core.Renderer;

public static class CameraUtils
{
    public static void CreateLightView(
        scoped ref CameraMatrices view,
        in ShadowParams shadowParams,
        Vector3 lightDirection,
        Span<Vector3> corners
    )
    {
        var dir = Vector3.Normalize(lightDirection);
        var worldUp = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

        var center = FrustumMath.GetFrustumCenter(corners);
        var farthestDistSqr = 0f;
        foreach (ref readonly var c in corners)
        {
            var d = Vector3.DistanceSquared(center, c);
            if (d > farthestDistSqr) farthestDistSqr = d;
        }

        var radius = MathF.Sqrt(farthestDistSqr);
        var diameter = radius * 2.0f;

        var shadowRotation = Matrix4x4.CreateLookAt(default, -dir, worldUp);
        var centerLs = Vector3.Transform(center, shadowRotation);

        var texelSize = diameter / shadowParams.ShadowMapSize;
        var snappedX = MathF.Floor(centerLs.X / texelSize) * texelSize;
        var snappedY = MathF.Floor(centerLs.Y / texelSize) * texelSize;

        var snappedCenterLs = new Vector3(snappedX, snappedY, centerLs.Z);

        Matrix4x4.Invert(shadowRotation, out var invShadowRotation);
        var snappedCenterWorld = Vector3.Transform(snappedCenterLs, invShadowRotation);

        var eye = snappedCenterWorld - dir * shadowParams.Distance * 0.5f;
        view.ViewMatrix = Matrix4x4.CreateLookAt(eye, snappedCenterWorld, worldUp);

        var minZ = float.MaxValue;
        var maxZ = float.MinValue;

        foreach (ref readonly var c in corners)
        {
            var z = Vector3.Transform(c, view.ViewMatrix).Z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        var nearLs = -maxZ - shadowParams.ZPad;
        var farLs = -minZ + shadowParams.ZPad;

        view.ProjectionMatrix = Matrix4x4.CreateOrthographic(
            diameter,
            diameter,
            nearLs,
            farLs
        );

        view.ProjectionViewMatrix = view.ViewMatrix * view.ProjectionMatrix;
    }
}