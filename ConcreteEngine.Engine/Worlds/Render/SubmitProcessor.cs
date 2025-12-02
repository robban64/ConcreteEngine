using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class SubmitProcessor
{
    public static int ExecuteSubmitTransform(int i, int writeIdx)
    {
        ref readonly var transform = ref DrawEntityStore.EntityData[i].Transform;
        var source = DrawEntityStore.Entities[i].Source;
        var model = source.Model;

        var locals = RenderDataContext.GetPartTransforms(model);
        var writer = RenderDataContext.GetDrawUploaderCtx();

        MatrixMath.CreateModelMatrix(transform.Translation, transform.Scale, transform.Rotation, out var world);

        var isAnimated = source.AnimatedSlot > 0;
        foreach (ref readonly var local in locals)
        {
            ref var modelTransform = ref writer.WriteBuffer(writeIdx++);
            ApplyTransform(ref modelTransform, in local, in world, isAnimated);
        }
        return writeIdx;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyTransform(ref DrawObjectUniform data, in Matrix4x4 locals, in Matrix4x4 world, bool isAnimated)
    {
        if (isAnimated)
            data.Model = world;
        else
            MatrixMath.WriteMultiplyAffine(ref data.Model, in locals, in world);

        MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
    }

}