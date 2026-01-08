using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class SpatialProcessor
{
    internal static int CullEntities(FrameEntityContext frameCtx, int[] byEntityId, in CameraRenderView renderView)
    {
        var renderEcsSpan = frameCtx.GetRenderEntitySpan();
        var worldSpan = new UnsafeSpan<Matrix4x4>(frameCtx.GetEntityWorldSpan());
        var count = 0;
        BoundingBox worldBounds;
        foreach (var query in Ecs.Render.CoreQuery())
        {
            ref readonly var transform = ref query.Transform;
            ref readonly var box = ref query.Box;
            ref readonly var parent = ref query.Parent;

            var entity = query.RenderEntity;
             var finalMatrix =  worldSpan[entity];

            MatrixMath.CreateModelMatrix(in transform.Transform, out var local);
            MatrixMath.WriteMultiplyAffine(ref finalMatrix.Value, in local, in parent.World);

            CameraUtils.GetWorldBounds(in box.Bounds, in finalMatrix.Value, out worldBounds);
            if (!renderView.Frustum.IntersectsBox(in worldBounds)) continue;
            byEntityId[entity] = count;
            renderEcsSpan[count++] = entity;
        }

        return count;
    }

    internal static void TagDepthKeys(in DrawEntityContext ctx, Camera camera)
    {
        var renderView = camera.RenderView;
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        var transformSpan = new UnsafeSpan<RenderTransform>(Ecs.Render.Core.GetTransformSpan());
        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            var translation = transformSpan.At(entity.RenderEntity).Transform.Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, in translation, nearFar);
            entity.Meta.DepthKey = depthKey;
        }
    }

    public static void UploadTransform(RenderContext renderCtx, in DrawEntityContext ctx,
        in DrawCommandUploader uploader)
    {
        var partView = renderCtx.MeshTable.GetTransformPartView();

        SpanSlice<Matrix4x4> slice = default;
        ModelId prevModel = default;
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            ref readonly var world = ref ctx.EntityWorld[entity.RenderEntity];

            if (prevModel != entity.Source.Model)
            {
                prevModel = entity.Source.Model;
                slice = partView.GetSlice(entity.Source.Model.Index());
            }

            foreach (var part in slice.Span)
            {
                ref var writer = ref uploader.GetWriter();
                if (entity.Source.AnimatedSlot > 0)
                    writer.Model = world;
                else
                    MatrixMath.WriteMultiplyAffine(ref writer.Model, in part, in world);
                MatrixMath.CreateNormalMatrix(in writer.Model, out writer.Normal);
            }
        }
    }
}