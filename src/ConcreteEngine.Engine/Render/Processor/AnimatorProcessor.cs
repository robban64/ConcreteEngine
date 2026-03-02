using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class AnimatorProcessor
{
    private static readonly NativeArray<Matrix4x4> Globals = new(RenderLimits.BoneCapacity);

    public static void Execute(AnimationTable animations, DrawCommandBuffer commandBuffer,
        ReadOnlySpan<int> byEntityId)
    {
        var uploader = commandBuffer.GetSkinningUploaderCtx();

        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (byEntityId[query.RenderEntity] == -1) continue;
            var it = query.Component;
            var clip = animations.GetAnimationData(it.Animation, it.Clip, out var skeleton);
            ExecuteInner(it.Time, in skeleton, clip, uploader.GetWriter());
        }
    }

    private static void ExecuteInner(float time, in SkeletonData skeleton, ReadOnlySpan<AnimationChannel> clip,
        UnsafeSpanSlice<Matrix4x4> writer)
    {
        var globals = Globals;

        SamplePose(0, time, skeleton.BindPose, clip[0], out globals.GetRef());
        MatrixMath.WriteMultiplyAffine(ref writer[0], in skeleton.InverseBindPose[0], in globals.GetRef());

        var len = skeleton.Length;
        for (var i = 1; i < len; i++)
        {
            var p = skeleton.ParentIndices[i];
            ref readonly var inverseBindPose = ref skeleton.InverseBindPose[i];
            ref var global = ref globals.GetRef(i);

            SamplePose(i, time, skeleton.BindPose, clip[i], out var local);
            MatrixMath.WriteMultiplyAffine(ref global, in local, in globals.GetRef(p));
            MatrixMath.WriteMultiplyAffine(ref writer[i], in inverseBindPose, in global);
        }
    }


    private static void SamplePose(int i, float time, Matrix4x4[] bindPose, AnimationChannel clip,
        out Matrix4x4 local)
    {
        var track = clip.GetTrackView();
        if (track.Length == 0)
        {
            local = bindPose[i];
            return;
        }

        var pos = SampleVector(time, track.PositionKeyFrames);
        var rot = SampleQuaternion(time, track.RotationKeyFrames);
        MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out local);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, UnsafeZippedSpan<float, Vector3> zip)
    {
        if (zip.Length == 1) return zip[0].Item2;

        var index = FindIndex(zip.GetSpanItem1(), time);

        var i0 = zip[index];
        var i1 = zip[index + 1];
        var factor = (time - i0.Item1) / (i1.Item1 - i0.Item1);
        return Vector3.Lerp(i0.Item2, i1.Item2, factor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion SampleQuaternion(float time, UnsafeZippedSpan<float, Quaternion> zip)
    {
        if (zip.Length == 1) return zip[0].Item2;

        var index = FindIndex(zip.GetSpanItem1(), time);

        var i0 = zip[index];
        var i1 = zip[index + 1];
        var factor = (time - i0.Item1) / (i1.Item1 - i0.Item1);

        return Quaternion.Slerp(i0.Item2, i1.Item2, factor);
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