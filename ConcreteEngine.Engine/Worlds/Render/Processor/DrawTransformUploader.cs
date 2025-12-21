using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTransformUploader
{
    public static void UploadTransform(in DrawEntityContext ctx, in DrawCommandUploader uploader,
        RenderEntityCore ecsCore, MeshTable meshTable)
    {
        var transformSpan = ecsCore.GetTransformSpan();
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            var index = entity.RenderEntity.Index();
            ref readonly var transform = ref transformSpan[index].Transform;

            MatrixMath.CreateModelMatrix(in transform, out var world);
            var locals = meshTable.GetPartTransforms(entity.Source.Model);
            foreach (ref readonly var local in locals)
            {
                WriteTransformUniform(ref uploader.GetWriter(), in local, in world, entity.Source.AnimatedSlot);
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