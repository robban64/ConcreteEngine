#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawTransformUploader
{
    public static void UploadTransform(DrawEntityContext ctx)
    {
        var view = DrawDataProvider.WorldEntities.Core.GetCoreView();
        
        Transform transform;
        foreach (var it in ctx)
        {
            ref readonly var entity = ref it.DrawEntity;
            if(entity.Source.Model <= 0) continue;
            var isAnimated = entity.Meta.AnimatedSlot > 0;

            transform = view.GetTransform(entity.Entity);

            ExecuteSubmitTransform(in transform, entity.Source.Model, isAnimated);
        }

    }


    private static void ExecuteSubmitTransform(in Transform t, ModelId model, bool isAnimated)
    {
        MatrixMath.CreateModelMatrix(in t.Translation, in t.Scale, in t.Rotation, out var world);
        
        var writer = DrawDataProvider.GetDrawUploaderCtx();
        var locals = DrawDataProvider.GetPartTransforms(model);
        foreach (ref readonly var local in locals)
        {
            ref var modelTransform = ref writer.GetWriter();
            WriteTransformUniform(ref modelTransform, in local, in world, isAnimated);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTransformUniform(ref DrawObjectUniform data, in Matrix4x4 locals, in Matrix4x4 world,
        bool isAnimated)
    {
        if (isAnimated)
            data.Model = world;
        else
            MatrixMath.MultiplyAffine(in locals, in world, out data.Model);

        MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
    }
}