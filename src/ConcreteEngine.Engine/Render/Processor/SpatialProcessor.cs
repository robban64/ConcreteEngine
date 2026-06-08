using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Buffer;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed class SpatialProcessor(CameraFrustum frustum, Camera camera)
{
    internal int CullEntities()
    {
        var index = 0;
        foreach (var query in Ecs.Render.Core.Query())
        {
            BoundingBox.GetWorldBounds(in query.Bounds, in query.Matrix, out var worldBounds);
            var visible = frustum.IntersectsBox(in worldBounds);

            visible &= query.ToggleVisibilityFlag(VisibilityFlags.Culled, visible) == 0;
            if (visible) index++;
            
        }

        return index;
    }

    internal void TagDepthKeys(DrawCommandContext ctx)
    {
        var near = camera.NearPlane;
        var far = camera.FarPlane;
        var forward = camera.Forward;
        var z = camera.ViewMatrix.M43;
        var transformView = Ecs.Render.Core.GetTransformView();
        foreach (var query in Ecs.Render.Core.VisibilityQuery())
        {
            ref readonly var worldPos = ref transformView[query.Entity.Index()].Translation;
            var depthKey = MakeDepthKey(worldPos, forward, z, near, far);

            ref var meta = ref ctx.GetMeta(query.VisibleIndex);
            meta.DepthKey = meta.Queue < DrawCommandQueue.Transparent
                ? depthKey
                : (ushort)(ushort.MaxValue - depthKey);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort MakeDepthKey(Vector3 worldPos,  Vector3 forward, float viewZ, float near, float far)
    {
        //var z = worldPos.X * view.X + worldPos.Y * view.Y + worldPos.Z * view.Z + viewZ;
        var d = Vector3.Dot(forward, worldPos) - viewZ;

        if (d <= near) return 0;
        if (d >= far) return ushort.MaxValue;

        var t = (d - near) / (far - near);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}