using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTagResolver
{
    internal static void TagDepthKeys(in DrawEntityContext ctx, in EntitiesReadView view,
        in CameraRenderView renderView)
    {
        var viewDepth = DepthKeyUtility.ExtractDepthVector(in renderView.ViewMatrix);
        var nearFar = new Vector2(renderView.ProjectionInfo.Near, renderView.ProjectionInfo.Far);

        foreach (var it in ctx)
        {
            ref var entity = ref it.DrawEntity;
            ref readonly var worldPos = ref view.GetTransform(entity.Entity).Translation;
            var depthKey = DepthKeyUtility.MakeDepthKey(in viewDepth, worldPos, nearFar);
            entity.Meta.DepthKey = depthKey;
        }
    }

    internal static void TagEffectResolvers(in DrawEntityContext ctx, WorldEntities worldEntities)
    {
        var deltaTime = EngineTime.DeltaTime;
        var slot = 1;
        foreach (var query in worldEntities.Query<AnimationComponent>())
        {
            var entityId = query.Entity;
            ref var component = ref query.Component;
            component.AdvanceTime(deltaTime);

            var index = ctx.ByEntityIdSpan[entityId];
            if (index == -1) continue;
            ref var drawEntity = ref ctx.EntitySpan[index];
            drawEntity.Source.AnimatedSlot = (ushort)slot++;
        }

        if(worldEntities.GetStore<SelectionComponent>().Count == 0) return;

        foreach (var query in worldEntities.Query<SelectionComponent>())
        {
            var entityId = query.Entity;
            var index = ctx.ByEntityIdSpan[entityId];
            if (index == -1) continue;
            ref readonly var component = ref query.Component;
            ref var drawEntity = ref ctx.EntitySpan[index];
            drawEntity.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
            drawEntity.Source.Resolver = DrawCommandResolver.Highlight;
        }
    }

    public static void UploadEffectCommands(in DrawEntityContext ctx, in DrawCommandUploader uploader,
        WorldEntities worldEntities, MeshTable meshTable)
    {
        if(worldEntities.GetStore<DebugBoundsComponent>().Count == 0) return;
        
        var view = worldEntities.Core.GetReadView();
        BoundingBox worldBounds;
        foreach (var query in worldEntities.Query<DebugBoundsComponent>())
        {
            var entityId = query.Entity;
            var index = ctx.ByEntityIdSpan[entityId];
            if (index == -1) continue;
            
            ref readonly var component = ref query.Component;
            ref readonly var drawEntity = ref ctx.EntitySpan[index];
            ref readonly var transform = ref view.GetTransform(entityId);

            if (!component.ByPart || meshTable.GetPartLengthFor(drawEntity.Source.Model) == 0)
            {
                ref readonly var bounds = ref view.GetBox(entityId);
                RenderTransform.GetWorldBounds(in bounds.Bounds, in transform, out worldBounds);
                uploader.SubmitDraw(default, default);
                return;
            }

            var slot = drawEntity.Source.AnimatedSlot;

            MatrixMath.CreateModelMatrix(in transform.Translation, in transform.Scale,
                in transform.Rotation, out var world);

            var partView = meshTable.GetPartsView(drawEntity.Source.Model);
            for(int i = 0; i < partView.Bounds.Length; i++)
            {
                ref readonly var local = ref partView.Locals[i];
                ref var writer = ref uploader.GetWriter();
                DrawTransformUploader.WriteTransformUniform(ref writer, in local, in world, slot);
                uploader.SubmitDraw(default, default);
            }
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