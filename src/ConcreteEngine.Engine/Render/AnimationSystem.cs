using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

internal sealed unsafe class AnimationSystem : IDisposable
{
    private NativeArray<Matrix4x4> _globals;

    private readonly AnimationManager _animations;
    private readonly SkinningBuffer _skinningBuffer;

    internal AnimationSystem(AnimationManager animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity, alignment: 16);
        _animations = animations;
        _skinningBuffer = skinningBuffer;
    }

    public void Dispose() => _globals.Dispose();

    public void Simulate(float dt)
    {
        foreach (var it in _animations)
            it.AdvanceTime(dt);
    }


    public void Execute()
    {
        ushort slot = 1;
        foreach (var animation in _animations)
        {
            var count = 0;
            foreach (var entity in animation.GetEntitySpan())
            {
                if (!Ecs.RenderCore.IsVisible(entity)) continue;
                Ecs.RenderCore.GetSource(entity).AnimationSlot = slot;
                ++count;
            }

            if (count == 0) continue;

            var time = animation.Interpolate();
            var skinningContext = animation.GetSkinningContext();
            WriteSkinned(time, skinningContext);
            ++slot;
        }
    }

    private void WriteSkinned(float time, SkinningContext ctx)
    {
        var globals = _globals.Ptr;
        for (var i = 0; i < ctx.Length; i++)
        {
            ref readonly var track = ref ctx.Tracks[i];
            if (track.IsEmpty)
            {
                globals[i] = ctx.GetBindPose(i);
                continue;
            }

            var posFactor = GetIndexFactor(time, track.PositionTimes, out var posIndex);
            var rotFactor = GetIndexFactor(time, track.RotationTimes, out var rotIndex);

            var pos = GetPosition(posIndex, posFactor, track.Positions);
            var rot = GetRotation(rotIndex, rotFactor, track.Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out globals[i]);
        }

        var writer = _skinningBuffer.WriteSlot(ctx.Length);

        MatrixMath.MultiplyAffine(ref writer[0], in ctx.GetInverseBindPose(0), in globals[0]);
        for (var i = 1; i < ctx.Length; i++)
        {
            var p = ctx.GetParentIndices(i);
            MatrixMath.MultiplyAffine(ref globals[i], in globals[p]);
            MatrixMath.MultiplyAffine(ref writer[i], in ctx.GetInverseBindPose(i), in globals[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetPosition(int posIndex, float posFactor, NativeView<Vector3> positions)
    {
        return posIndex > 0
            ? Vector3.Lerp(positions[posIndex], positions[posIndex + 1], posFactor)
            : positions[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(int rotIndex, float rotFactor, NativeView<Quaternion> rotation)
    {
        return rotIndex > 0
            ? Quaternion.Slerp(rotation[rotIndex], rotation[rotIndex + 1], rotFactor)
            : rotation[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetIndexFactor(float time, NativeView<float> times, out int index)
    {
        if (times.Length == 1)
        {
            index = -1;
            return 0;
        }

        index = FindIndex(times, time);
        var i0 = times[index];
        var i1 = times[index + 1];
        return (time - i0) / (i1 - i0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex(NativeView<float> keys, float time)
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