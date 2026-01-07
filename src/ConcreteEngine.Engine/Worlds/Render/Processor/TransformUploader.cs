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