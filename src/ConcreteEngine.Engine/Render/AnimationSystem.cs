using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

internal sealed unsafe class AnimationSystem : IDisposable
{
    private int _frameCount;
    private (RenderEntityId entity, ushort slot)[] _entitySlots;

    private NativeArray<Matrix4x4> _globals;

    private readonly AnimationManager _animations;
    private readonly SkinningBuffer _skinningBuffer;

    internal AnimationSystem(AnimationManager animations, SkinningBuffer skinningBuffer)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(RenderLimits.BoneCapacity, alignment: 16);
        _animations = animations;
        _skinningBuffer = skinningBuffer;
        _entitySlots = new (RenderEntityId entity, ushort slot)[64];
    }

    public void Dispose() => _globals.Dispose();

    public void Simulate(float dt)
    {
        foreach (var it in _animations) it.AdvanceTime(dt);
    }

    public void Execute(float alpha)
    {
        _frameCount = 0;
        var cursor = 0;
        ushort slot = 1;
        foreach (var animation in _animations)
        {
            var count = FilterEntities(animation, cursor, slot);
            if (count == 0) continue;

            var time = animation.Interpolate(alpha);
            WriteSkinned(animation.GetSkinningContext(), time);
            ++slot;
            cursor += count;
        }

        _frameCount = cursor;
    }

    private int FilterEntities(AnimationInstance animation, int cursor, ushort slot)
    {
        var count = 0;
        foreach (var entity in animation.GetEntitySpan())
        {
            if (!Ecs.RenderCore.IsVisible(entity)) continue;
            
            var index = cursor + count;
            if((uint)index >= (uint)_entitySlots.Length)
                Array.Resize(ref _entitySlots, _entitySlots.Length * 2);
            
            _entitySlots[index] = (entity, slot);
            ++count;
        }

        return count;
    }

    public void WriteCommandSlot(DrawCommandBuffer cmd, ReadOnlySpan<RenderEntityId> visibleEntities)
    {
        var length = _frameCount;
        for (int i = 0; i < length; ++i)
        {
            var entitySlot = _entitySlots[i];
            var index = visibleEntities.BinarySearch(entitySlot.entity);
            if (index >= 0) cmd.CommandRef(index).AnimationSlot = entitySlot.slot;
        }
    }


    private void WriteSkinned(SkinningContext ctx, float time)
    {
        var globals = _globals.Ptr;
        var track = ctx.Tracks.BoneTracks;
        var length = ctx.Tracks.Length;
        
        for (var i = 0; i < length; ++i, ++track, ++globals)
        {
            if (track->IsEmpty)
            {
                *globals = ctx.GetBindPose(i);
                continue;
            }

            var posFactor = GetIndexFactor(time, track->PositionTimes, out var posIndex);
            var rotFactor = GetIndexFactor(time, track->RotationTimes, out var rotIndex);

            var pos = GetPosition(posIndex, posFactor, track->Positions);
            var rot = GetRotation(rotIndex, rotFactor, track->Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out *globals);
        }

        globals = _globals.Ptr;
        var dst = _skinningBuffer.WriteSlot(length).Ptr;
        MatrixMath.MultiplyAffine(ref *++dst, in ctx.GetInverseBindPose(0), in globals[0]);
        for (var i = 1; i < length; ++i, ++dst)
        {
            var p = ctx.GetParentIndices(i);
            MatrixMath.MultiplyAffine(ref globals[i], in globals[p]);
            MatrixMath.MultiplyAffine(ref *dst, in ctx.GetInverseBindPose(i), in globals[i]);
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
            int mid = lo + ((hi - lo) >>> 1);
            int cmp = keys[mid].CompareTo(time);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        int idx = hi;
        return int.Clamp(idx, 0, keys.Length - 2);
    }
}