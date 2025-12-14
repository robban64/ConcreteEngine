#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTransformUploader
{
    public static void UploadTransform(DrawEntityContext ctx)
    {
        var view = DrawDataProvider.WorldEntities.Core.GetCoreView();
        var writer = DrawDataProvider.GetDrawUploaderCtx();

        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            var animatedSlot = entity.Meta.AnimatedSlot;
            if (entity.Source.Model <= 0) continue;

            ref readonly var t = ref view.GetTransform(entity.Entity);

            MatrixMath.CreateModelMatrix(in t.Translation, in t.Scale, in t.Rotation, out var world);
            var locals = DrawDataProvider.GetPartTransforms(entity.Source.Model);
            foreach (ref readonly var local in locals)
            {
                ref var modelTransform = ref writer.GetWriter();
                WriteTransformUniform(ref modelTransform, in local, in world, animatedSlot);
            }

        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTransformUniform(ref DrawObjectUniform data, in Matrix4x4 locals, in Matrix4x4 world,
        ushort animationSlot)
    {
        if (animationSlot > 0)
            data.Model = world;
        else
            MatrixMath.MultiplyAffine(in locals, in world, out data.Model);

        MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
    }
}