using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class TransformUploader
{
    public static void UploadTransform(in DrawEntityContext ctx, in DrawCommandUploader uploader, MeshTable meshTable)
    {
        var partView = meshTable.GetTransformPartView();

        SpanSlice<Matrix4x4> slice = default;
        ModelId prevModel = default;
        var world = Matrix4x4.Identity;
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            ref readonly var transform = ref Ecs.Render.Core.GetTransform(entity.RenderEntity);
            ref readonly var parentMatrix = ref Ecs.Render.Core.GetParentMatrix(entity.RenderEntity);

            MatrixMath.CreateModelMatrix(in transform.Transform, out var model);
            MatrixMath.WriteMultiplyAffine(ref world, in model, in parentMatrix.World);

            if (prevModel != entity.Source.Model)
            {
                prevModel = entity.Source.Model;
                slice = partView.GetSlice(entity.Source.Model.Index());
            }
            
            foreach (var part in slice.Span)
            {
                ref var writer = ref uploader.GetWriter();
                if (entity.Source.AnimatedSlot > 0)
                    writer.Model = world;
                else
                    MatrixMath.WriteMultiplyAffine(ref writer.Model, in part, in world);
                MatrixMath.CreateNormalMatrix(in writer.Model, out writer.Normal);
            }
        }
    }
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTransformUniform(ref DrawObjectUniform data, in ValuePtr<Matrix4x4> localPtr,
        in Matrix4x4 world, ushort animationSlot)
    {
        if (animationSlot > 0)
            data.Model = world;
        else
            MatrixMath.WriteMultiplyAffine(ref data.Model, in localPtr.Value, in world);
    }
*/
}