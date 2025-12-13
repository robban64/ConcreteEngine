#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTagResolver
{
    internal static void TagEffectResolvers(DrawEntityContext ctx)
    {
        var worldEntities = DrawDataProvider.WorldEntities;
        var dt = DrawDataProvider.DeltaTime;

        foreach (var resolved in worldEntities.ResolvedEntitySpan)
        {
            var index = ctx.ByEntityIdSpan[resolved.Entity];
            if(index == -1) continue;
            ref var drawEntity = ref ctx.EntitySpan[index];
            drawEntity.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
            drawEntity.Meta.Resolver = resolved.CommandResolver;
        }

        int slot = 1;
        foreach (var query in DrawDataProvider.WorldEntities.Query<AnimationComponent>())
        {
            var entityId = query.Entity;
            ref var component = ref query.Component;
            component.AdvanceTime(dt);

            var index = ctx.ByEntityIdSpan[entityId];
            if(index == -1) continue;
            ref var drawEntity = ref ctx.EntitySpan[index];
            drawEntity.SetAnimationSlot(slot++);
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