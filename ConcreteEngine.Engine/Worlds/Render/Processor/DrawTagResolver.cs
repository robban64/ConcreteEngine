#region

using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTagResolver
{
    internal static void TagEffectResolvers(DrawEntityContext ctx)
    {
        foreach (var query in DrawDataProvider.WorldEntities.Query<AnimationComponent>())
        {
            ref var entity = ref ctx.GetByEntityId(query.Entity);
            entity.SetAnimationSlot(query.Index + 1);
        }

        var selected = WorldInteractive.SelectedEntityId;
        if (selected > 0)
        {
            var idx = ctx.ByEntityIdSpan[selected];
            ref var entity = ref ctx.EntitySpan[idx];
            entity.Meta.PassMask = PassMask.Effect | PassMask.DepthPre;
            entity.Meta.Resolver = entity.Meta.AnimatedSlot == 0
                ? DrawCommandResolver.Highlight
                : DrawCommandResolver.HighlightAnimated;
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