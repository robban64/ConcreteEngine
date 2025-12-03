using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawCommandProcessor
{
    public static void ExecuteSubmitCommand(int idx, in DrawEntity entity, in MaterialTag materialTag)
    {
        var parts = DrawDataProvider.GetMeshParts(entity.Source.Model);
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        foreach (ref readonly var part in parts)
        {
            var isTransparent = materialTag.GetTagMeta(part.MaterialSlot, out var materialId);

            var cmd = new DrawCommand(part.Mesh, materialId, drawCount: part.DrawCount,
                animationSlot: entity.Source.AnimatedSlot);

            var meta = BuildMeta(isTransparent, entity.Meta);

            writer.SubmitDraw(in cmd, meta);
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DrawCommandMeta BuildMeta(bool isTransparent, DrawEntityMeta m)
        {
            if (!isTransparent)
                return m.ToCommandMeta();

            var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
            return new DrawCommandMeta(m.CommandId, DrawCommandQueue.Transparent, m.Resolver, m.PassMask, depthKey);
        }
    }

    public static void ExecuteSubmitTransform(int idx)
    {
        ref readonly var transform = ref DrawEntityStore.EntityData[idx].Transform;
        var source = DrawEntityStore.Entities[idx].Source;
        var model = source.Model;

        var locals = DrawDataProvider.GetPartTransforms(model);
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        MatrixMath.CreateModelMatrix(transform.Translation, transform.Scale, transform.Rotation, out var world);

        var isAnimated = source.AnimatedSlot > 0;
        foreach (ref readonly var local in locals)
        {
            ref var modelTransform = ref writer.GetWriter();
            WriteTransformUniform(ref modelTransform, in local, in world, isAnimated);
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteTransformUniform(ref DrawObjectUniform data, in Matrix4x4 locals, in Matrix4x4 world,
            bool isAnimated)
        {
            if (isAnimated)
                data.Model = world;
            else
                MatrixMath.WriteMultiplyAffine(ref data.Model, in locals, in world);

            MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
        }
    }
}