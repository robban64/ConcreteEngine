#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityCulling
{
    internal static int CullEntities(EntityId[] entityIndices, int[] byEntityId)
    {
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in DrawDataProvider.WorldEntities.CoreQuery())
        {
            GetWorldBounds(in query.Box.Bounds, in query.Transform, out worldBounds);
            if (!DrawDataProvider.Frustum.IntersectsBox(in worldBounds)) continue;

            byEntityId[query.Entity] = count;
            entityIndices[count++] = query.Entity;
        }

        return count;
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetWorldBounds(
        in BoundingBox local,
        in Transform transform,
        out BoundingBox world)
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
}