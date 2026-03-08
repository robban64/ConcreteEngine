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

internal sealed class AnimatorProcessor(AnimationTable animations, DrawCommandBuffer buffer)
{
    private static readonly NativeArray<Matrix4x4> Globals =
        NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity);

    public void Execute(ReadOnlySpan<int> byEntityId)
    {
        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (byEntityId[query.RenderEntity] == -1) continue;
            var it = query.Component;
            var clip = animations.GetAnimationData(it.Animation, it.Clip, out var skeleton);
            ExecuteInner(it.Time, in skeleton, clip);
        }
    }

    private void ExecuteInner(float time, in SkeletonData skeleton, ReadOnlySpan<AnimationChannel> clip)
    {
        var writer = buffer.GetBoneWriter();

        SamplePose(0, time, skeleton.BindPose, in clip[0], out Globals[0]);
        MatrixMath.WriteMultiplyAffine(ref writer[0], in skeleton.InverseBindPose[0], in Globals[0]);

        var len = skeleton.ParentIndices.Length;
        for (var i = 1; i < len; i++)
        {
            var p = skeleton.ParentIndices[i];
            ref readonly var inverseBindPose = ref skeleton.InverseBindPose[i];

            SamplePose(i, time, skeleton.BindPose, in clip[i], out var local);
            MatrixMath.WriteMultiplyAffine(ref Globals[i], in local, in Globals[p]);
            MatrixMath.WriteMultiplyAffine(ref writer[i], in inverseBindPose, in Globals[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SamplePose(int i, float time, Matrix4x4[] bindPose, in AnimationChannel clip,
        out Matrix4x4 local)
    {
        if (clip.MaxLength == 0)
        {
            local = bindPose[i];
            return;
        }

        var posIndex = GetIndexFactor(time, new UnsafeSpan<float>(clip.PositionTimes), out var posFactor);
        var rotIndex = GetIndexFactor(time, new UnsafeSpan<float>(clip.RotationTimes), out var rotFactor);

        var pos = posIndex > 0
            ? Vector3.Lerp(clip.Positions[posIndex], clip.Positions[posIndex + 1], posFactor)
            : clip.Positions[0];

        var rot = rotIndex > 0
            ? Quaternion.Slerp(clip.Rotations[rotIndex], clip.Rotations[rotIndex + 1], rotFactor)
            : clip.Rotations[0];

        MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out local);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexFactor(float time, UnsafeSpan<float> times, out float factor)
    {
        if (times.Length == 1)
        {
            factor = 0;
            return -1;
        }

        var index = FindIndex(times, time);
        var i0 = times[index];
        var i1 = times[index + 1];
        factor = (time - i0) / (i1 - i0);
        return index;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 SampleVector(float time, UnsafeSpan<float> times, Span<Vector3> values)
    {
        if (times.Length == 1) return values[0];

        var index = FindIndex(times, time);

        var i0 = times[index];
        var i1 = times[index + 1];
        var factor = (time - i0) / (i1 - i0);
        return Vector3.Lerp(values[index], values[index + 1], factor);
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
        if (time >= keys[keys.Length - 1]) return keys.Length - 2;
        if (time <= keys[0]) return 0;

        int lo = 0, hi = keys.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = keys[mid].CompareTo(time);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        int idx = hi;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
}