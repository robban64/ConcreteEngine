using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTagResolver
{
    internal static void TagResolveEntities(in DrawEntityContext ctx)
    {
        var slot = 0;
        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            var drawPtr = ctx.TryGetVisible(query.RenderEntity);
            if (drawPtr.IsNull) continue;
            drawPtr.Value.Source.AnimatedSlot = (ushort)++slot;
        }

        if (Ecs.Render.Stores<SelectionComponent>.Store.Count == 0) return;

        foreach (var query in Ecs.Render.Query<SelectionComponent>())
        {
            var drawPtr = ctx.TryGetVisible(query.RenderEntity);
            if (drawPtr.IsNull) continue;
            //ref readonly var component = ref query.Component;
            drawPtr.Value.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
            drawPtr.Value.Source.Resolver = DrawCommandResolver.Highlight;
        }
    }

    public static void UploadDebugBounds(in DrawEntityContext ctx, in DrawCommandUploader uploader, MeshTable meshTable,
        MaterialId materialId)
    {
        if (Ecs.Render.Stores<DebugBoundsComponent>.Store.Count == 0) return;

        var view = Ecs.Render.Core.GetContext();
        Span<Vector3> corners = stackalloc Vector3[8];
        Matrix4x4 world;
        foreach (var query in Ecs.Render.Query<DebugBoundsComponent>())
        {
            var entityId = query.RenderEntity;
            var index = ctx.ByEntityIdSpan[entityId];
            if (index == -1) continue;

            ref readonly var component = ref query.Component;
            ref readonly var drawEntity = ref ctx.EntitySpan[index];
            ref readonly var transform = ref view.GetTransform(entityId).Transform;
            ref readonly var bounds = ref view.GetBox(entityId).Bounds;

            var depthKey = (ushort)(ushort.MaxValue - drawEntity.Meta.DepthKey);
            var cmd = new DrawCommand(PrimitiveMeshes.Cube, materialId, resolver: DrawCommandResolver.BoundingVolume);
            var meta = new DrawCommandMeta(DrawCommandId.Effect, DrawCommandQueue.Effect, PassMask.Effect, depthKey);

            MatrixMath.CreateModelMatrix(in transform, out world);

            if (!component.ByPart)
            {
                ref var writer = ref uploader.GetWriter();
                CreateBoxMatrix(corners, in bounds, in transform, in world, out writer.Model);
                writer.Normal = default;
                uploader.SubmitDraw(cmd, meta);
                return;
            }

            var partView = meshTable.GetModelPartView(drawEntity.Source.Model);
            foreach (ref readonly var local in partView.Bounds)
            {
                ref var writer = ref uploader.GetWriter();
                CreateBoxMatrix(corners, in local, in transform, in world, out writer.Model);
                writer.Normal = default;
                uploader.SubmitDraw(cmd, meta);
            }
        }

        return;

        static void CreateBoxMatrix(Span<Vector3> corners, in BoundingBox local, in Transform transform,
            in Matrix4x4 world, out Matrix4x4 global)
        {
            local.FillCorners(corners);
            for (var i = 0; i < corners.Length; i++)
                corners[i] = Vector3.Transform(corners[i], world);

            BoundingAxisBox.FromPoints(corners, out var axisBounds);

            MatrixMath.CreateModelMatrix(in axisBounds.Center, in axisBounds.Extent, in transform.Rotation, out global);
        }
    }
/*
    private bool hasRunEntities = false;
    private void DrawBounds()
    {
        if (hasRunEntities)
        {
            _idx *= 2;
            return;
        }

        var idx = _idx;
        Span<Vector3> corners = stackalloc Vector3[8];
        foreach (ref readonly var entity in _entities.AsSpan(0, idx))
        {
            ref var boxEntity = ref _entities[_idx++];
            ref readonly var bounds = ref _world.Entities.BoundingBoxes.GetById(entity.Entity);
            ref readonly var transform = ref entity.Transform;

            MatrixMath.CreateModelMatrix(in transform.Translation, in transform.Scale,
                in transform.Rotation, out var world);

            bounds.Box.FillCorners(corners);

            for (var i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], world);
            }

            BoundingAxisBox.FromPoints(corners, out var axisBounds);

            boxEntity.Entity = entity.Entity;
            boxEntity.Model = CubeId;
            boxEntity.MaterialKey = EmptyMaterialKey;
            boxEntity.Transform = new Transform(axisBounds.Center, axisBounds.Extent, in transform.Rotation);
            boxEntity.Meta = new DrawCommandMeta(DrawCommandId.Model, DrawCommandQueue.OverlayTransparent,
                DrawCommandResolver.BoundingVolume, PassMask.Effect);
        }

        hasRunEntities = true;
    }
*/
}