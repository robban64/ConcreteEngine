using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal sealed unsafe class AnimatorProcessor : IDisposable
{
    private NativeArray<Matrix4x4> _globals;
    private readonly DrawCommandBuffer _buffer;

    private readonly AnimationTable _animations;
    private readonly RenderEntityCore _ecs;

    public AnimatorProcessor(AnimationTable animations, DrawCommandBuffer buffer)
    {
        _globals = NativeArray.Allocate<Matrix4x4>(RenderLimits.BoneCapacity);
        _animations = animations;
        _buffer = buffer;
        _ecs = Ecs.Render.Core;
    }
    
    public void Dispose() => _globals.Dispose();


    public void Execute()
    {
        foreach (var query in Ecs.Render.Query<RenderAnimationComponent>())
        {
            if (!_ecs.IsVisible(query.Entity)) continue;
            var it = query.Component;
            var clip = _animations.GetAnimationData(it.Animation, it.Clip, out var skeleton);
            ExecuteInner(it.Time, skeleton, clip);
        }
    }

    private void ExecuteInner(float time, SkeletonMatrices skeleton, ReadOnlySpan<AnimationClipChannel> clips)
    {
        var writer = _buffer.WriteBones();
        var globals = _globals.Ptr;
        
        var len = int.Min(skeleton.ParentIndices.Length, clips.Length);
        for (var i = 0; i < len; i++)
        {
            ref readonly var clip = ref clips[i];
            if (clip.MaxLength == 0)
            {
                globals[i] = skeleton.BindPose[i];
                continue;
            }

            var pos = GetPosition(time, clip);
            var rot = GetRotation(time, clip);
            MatrixMath.CreateFixedSizeModelMatrix(pos, rot, out globals[i]);
        }

        writer[0] = skeleton.InverseBindPose[0] * globals[0];
        for (var i = 1; i < len; i++)
        {
            var p = skeleton.ParentIndices[i];
            globals[i] *= globals[p];
            writer[i] = skeleton.InverseBindPose[i] * globals[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetPosition(float time, in AnimationClipChannel clip)
    {
        var posIndex = GetIndexFactor(time, clip.GetPositionTimes(), out var posFactor);
        return posIndex > 0
            ? Vector3.Lerp(clip.Positions[posIndex], clip.Positions[posIndex + 1], posFactor)
            : clip.Positions[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(float time, in AnimationClipChannel clip)
    {
        var rotIndex = GetIndexFactor(time, clip.GetRotationTimes(), out var rotFactor);
        return rotIndex > 0
            ? Quaternion.Slerp(clip.Rotations[rotIndex], clip.Rotations[rotIndex + 1], rotFactor)
            : clip.Rotations[0];
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
/*

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
*/
}