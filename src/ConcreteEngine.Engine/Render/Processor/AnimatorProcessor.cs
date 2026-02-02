using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class AnimatorProcessor
{
    private static readonly NativeArray<Matrix4x4> Globals = new(RenderLimits.BoneCapacity);

    [SkipLocalsInit]
    public static void Execute(AnimationTable animations, DrawCommandBuffer commandBuffer, UnsafeSpan<int> byEntityId)
    {
        var uploader = commandBuffer.GetSkinningUploaderCtx();
        var globals = Globals;

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (byEntityId[query.RenderEntity] == -1) continue;

            var it = query.Component;

            //var view = dataView.GetModelView(it.Animation, out var invTransform);
            //var clip = view.GetClip(it.Clip);
            
            var entry = animations.GetAnimation(it.Animation);
            var len = entry.BoneCount;
            var clip = entry.Clips[it.Clip];
            ref readonly var skeleton = ref entry.Skeleton;

            var writer = uploader.GetWriter();
            
            ProcessRootBone(it.Time,
                clip[0],
                in skeleton.InverseBindPose[0],
                in skeleton.BindPose[0],
                in writer,
                in skeleton.InverseRoot,
                out globals.GetRef());

            Matrix4x4 skinMatrix = default;
            for (int i = 1; i < len; i++)
            {
                ref readonly var offset = ref skeleton.InverseBindPose[i];
                ref readonly var node = ref skeleton.BindPose[i];
                int p = skeleton.ParentIndices[i];

                SampleTrack(it.Time, clip[i], in node, out var local);

                ref var outputMatrix = ref writer[i];
                ref var globalCurrent = ref globals.GetRef(i);
                MatrixMath.WriteMultiplyAffine(ref globalCurrent, in local, in globals.GetRef(p));
                MatrixMath.WriteMultiplyAffine(ref skinMatrix, in offset, in globalCurrent);
                MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in skeleton.InverseRoot);
            }
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRootBone(float time, AnimationChannel channel, in Matrix4x4 offset, in Matrix4x4 node,
            in SpanSlice<Matrix4x4> writer, in Matrix4x4 invTransform, out Matrix4x4 global)
        {
            SampleTrack(time, channel, in node, out global);

            ref var outputMatrix = ref writer[0];
            MatrixMath.MultiplyAffine(in offset, in global, out var skinMatrix);
            MatrixMath.WriteMultiplyAffine(ref outputMatrix, in skinMatrix, in invTransform);
        }

        static void SampleTrack(float time, AnimationChannel channel, in Matrix4x4 node, out Matrix4x4 local)
        {
            if (channel.MaxLength == 0)
            {
                local = node;
                return;
            }

            var pos = SampleVector(time, new UnsafeSpan<float>(channel.PositionTimes), channel.Positions);
            var rot = SampleQuaternion(time, new UnsafeSpan<float>(channel.RotationTimes), channel.Rotations);
            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out local);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, UnsafeSpan<float> times, ReadOnlySpan<Vector3> values)
    {
        if(values.Length == 1) return values[0];
        
        int index = FindIndex(times, time);
        
        float t1 = times[index];
        float t2 = times[index + 1];
        float factor = (time - t1) / (t2 - t1);
        
        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];
        return Vector3.Lerp(k1, k2, factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion SampleQuaternion(float time, UnsafeSpan<float> times, ReadOnlySpan<Quaternion> values)
    {
        if(values.Length == 1) return values[0];

        int index = FindIndex(times, time);

        float t1 = times[index];
        float t2 = times[index + 1];
        float factor = (time - t1) / (t2 - t1);
        
        ref readonly var k1 = ref values[index];
        ref readonly var k2 = ref values[index + 1];
        return Quaternion.Slerp(k1, k2, factor);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex(UnsafeSpan<float> keys, float time)
    {
        if (time >= keys.At(keys.Length - 1).Value) return keys.Length - 2;
        if (time <= keys.At(0).Value) return 0;

        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = keys.At(mid).Value.CompareTo(time);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        int idx = hi;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
    
}