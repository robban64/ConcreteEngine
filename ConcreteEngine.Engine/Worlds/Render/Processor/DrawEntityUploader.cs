using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawEntityUploader
{
    public static void ExecuteGeneratedCommand(int idx, in DrawEntity entity)
    {
        var writer = DrawDataProvider.GetDrawUploaderCtx();
        var mesh = new MeshId(entity.Source.Model);
        var material = new MaterialId(entity.Source.MaterialKey.Value);
        var cmd = new DrawCommand(mesh, material, entity.Source.DrawCount, entity.Source.InstanceCount);
        writer.SubmitDrawIdentity(cmd, entity.Meta.ToCommandMeta());
    }


    public static void ExecuteSubmitCommand(int idx, in DrawEntity entity, in MaterialTag materialTag)
    {
        var parts = DrawDataProvider.GetMeshParts(entity.Source.Model);
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        foreach (ref readonly var part in parts)
        {
            var isTransparent = materialTag.ResolveSlot(part.MaterialSlot, out var materialId);

            var cmd = new DrawCommand(part.Mesh, materialId, part.DrawCount, entity.Source.InstanceCount,
                entity.Meta.AnimatedSlot);

            var meta = BuildMeta(entity.Meta, isTransparent);

            writer.SubmitDraw(in cmd, meta);
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DrawCommandMeta BuildMeta(DrawEntityMeta m, bool isTransparent)
        {
            if (!isTransparent)
                return m.ToCommandMeta();

            var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
            var queue = m.Queue >= DrawCommandQueue.Transparent ? m.Queue : DrawCommandQueue.Transparent;
            return new DrawCommandMeta(m.CommandId, queue, m.Resolver, m.PassMask, depthKey);
        }
    }

    public static void ExecuteSubmitTransform(int idx, in DrawEntity entity, in DrawEntityData entityData)
    {
        var locals = DrawDataProvider.GetPartTransforms(entity.Source.Model);
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        MatrixMath.CreateModelMatrix(entityData.Transform.Translation, entityData.Transform.Scale,
            entityData.Transform.Rotation, out var world);

        var isAnimated = entity.Meta.AnimatedSlot > 0;
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
                MatrixMath.MultiplyAffine(in locals, in world, out data.Model);

            MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
        }
    }
}