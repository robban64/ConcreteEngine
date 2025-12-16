using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTransformUploader
{
    public static void UploadTransform(in DrawEntityContext ctx, in EntitiesReadView view, in DrawCommandUploader uploader, MeshTable meshTable)
    {
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            ref readonly var t = ref view.GetTransform(entity.Entity);
            var animatedSlot = entity.Meta.AnimatedSlot;

            MatrixMath.CreateModelMatrix(in t.Translation, in t.Scale, in t.Rotation, out var world);
            var locals = meshTable.GetPartTransforms(entity.Source.Model);
            foreach (ref readonly var local in locals)
            {
                WriteTransformUniform(ref uploader.GetWriter(), in local, in world, animatedSlot);
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
            MatrixMath.WriteMultiplyAffine(ref data.Model, in locals, in world);

        MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
    }
}