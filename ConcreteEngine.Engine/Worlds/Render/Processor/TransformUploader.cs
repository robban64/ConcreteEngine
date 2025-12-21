using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Generics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class TransformUploader
{
    public static void UploadTransform(in DrawEntityContext ctx, in DrawCommandUploader uploader, MeshTable meshTable)
    {
        var partView = meshTable.GetTransformPartView();

        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            var index = entity.Source.Model.Index();
            var transformPtr = GenericStore.CoreStore.TryGetTransform(entity.RenderEntity);
            if (transformPtr.IsNull) continue;

            ref readonly var transform = ref transformPtr.Value;
            MatrixMath.CreateModelMatrix(in transform.Transform, out var world);

            var slot = entity.Source.AnimatedSlot;
            var slice = partView.GetSlice(index);
            foreach (var partPtr in slice)
            {
                ref var writer = ref uploader.GetWriter();
                WriteTransformUniform(ref writer, in partPtr, in world, slot);
                MatrixMath.CreateNormalMatrix(in writer.Model, out writer.Normal);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTransformUniform(ref DrawObjectUniform data, in ValuePtr<Matrix4x4> localPtr, in Matrix4x4 world,
        ushort animationSlot)
    {
        if (animationSlot > 0)
            data.Model = world;
        else
            MatrixMath.WriteMultiplyAffine(ref data.Model, in localPtr.Value, in world);
    }
}