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
        var transformSpan = DrawDataProvider.WorldEntities.Core.GetTransformSpan();
        var entities = ctx.EntitySpan;

        var len = entities.Length;
        if ((uint)len > transformSpan.Length) throw new IndexOutOfRangeException();
        
        Transform transform;
        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            if(entity.Source.Model <= 0) continue;
            var isAnimated = entity.Meta.AnimatedSlot > 0;

            transform = transformSpan[i];

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