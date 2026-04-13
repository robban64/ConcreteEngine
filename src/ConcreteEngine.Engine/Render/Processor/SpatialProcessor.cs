using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Renderer;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(Span<RenderEntityId> entities, Span<int> indices, Camera camera)
    {
        var index = 0;
        ref readonly var frustum = ref camera.GetFrustum();
        foreach (var query in Ecs.Render.Core.Query())
        {
            BoundingBox.GetWorldBounds(in query.Box, in query.Parent, out var worldBounds);
            var visible = frustum.IntersectsBox(in worldBounds);
            visible &= query.ToggleVisibilityFlag( VisibilityFlags.Culled, visible) == 0;
            var entityIndex = query.Entity.Index();
            if (!visible)
            {
                indices[entityIndex] = -1;
                continue;
            }

            indices[entityIndex] = index;
            entities[index] = query.Entity;
            index++;
        }

        return index;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, Camera camera)
    {
        var viewDepth = ExtractDepthVector(in camera.ViewMatrix);
        var nearFar = new Vector2(camera.NearPlane, camera.FarPlane);
        var transformView = Ecs.Render.Core.GetTransformView();
        foreach (var it in ctx)
        {
            ref readonly var transform = ref transformView[it.Entity.Index()];
            var depthKey = MakeDepthKey(in viewDepth, in transform.Translation, nearFar);

            it.Meta.DepthKey = it.Meta.Queue < DrawCommandQueue.Transparent
                ? depthKey
                : (ushort)(ushort.MaxValue - depthKey);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ExtractDepthVector(in Matrix4x4 v) => new(v.M13, v.M23, v.M33, v.M43);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort MakeDepthKey(in Vector4 view, in Vector3 worldPos, Vector2 nearFar)
    {
        var z = worldPos.X * view.X + worldPos.Y * view.Y + worldPos.Z * view.Z + view.W;
        var d = -z;

        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return ushort.MaxValue;

        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}