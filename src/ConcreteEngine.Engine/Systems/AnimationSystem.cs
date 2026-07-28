using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Buffers;
using static ConcreteEngine.Engine.Render.RenderLimits;

namespace ConcreteEngine.Engine.Systems;

internal sealed unsafe class AnimationSystem : IDisposable
{
    private const int DefaultCapacity = 64;
    private const int DefaultBoneBufferCap = BoneCapacity * 64;

    public int Count { get; private set; }
    public int BoneCount { get; private set; }

    private NativeArray<Matrix4x4> _globals;
    private NativeArray<Matrix4x4> _boneBuffer;
    private Range32[] _slotRanges;

    private readonly AnimationManager _animations;

    internal AnimationSystem(AnimationManager animations)
    {
        _globals = NativeArray.AlignedAllocate<Matrix4x4>(BoneCapacity, alignment: 16);
        _boneBuffer = NativeArray.AlignedAllocate<Matrix4x4>(DefaultBoneBufferCap, alignment: 16);
        _slotRanges = new Range32[DefaultCapacity];

        _animations = animations;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Range32 GetSlotRange(int slot) => _slotRanges[slot];
    
    public NativeView<DrawAnimationUniform> GetBufferView()
    {
        if (BoneCount == 0) return NativeView<DrawAnimationUniform>.MakeNull();
        if ((uint)BoneCount >= (uint)_boneBuffer.Length) Throwers.InvalidOperation();
        return new NativeView<DrawAnimationUniform>((DrawAnimationUniform*)_boneBuffer.Ptr, BoneCount);
    }

    public void ResetFrame()
    {
        Count = 0;
        BoneCount = 0;
    }

    public void Simulate(float dt)
    {
        foreach (var it in _animations) it.AdvanceTime(dt);
    }

    public void Execute(float alpha)
    {
        ushort slot = 1;
        foreach (var animation in _animations)
        {
            var count = FilterEntities(animation, slot);
            if (count == 0) continue;

            var time = animation.Interpolate(alpha);
            WriteSkinned(animation.GetSkinningContext(), time);
            
            ++slot;
        }
    }

    public void Dispose()
    {
        _globals.Dispose();
        _boneBuffer.Dispose();
    }

    private int FilterEntities(AnimationInstance animation, ushort slot)
    {
        var count = 0;
        foreach (var entity in animation.GetEntitySpan())
        {
            if (!Ecs.RenderCore.IsVisible(entity)) continue;
            Ecs.GetRenderStore<SkinningComponent>().Get(entity).AnimationSlot = slot;
            ++count;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NativeView<Matrix4x4> WriteSlot(int bones)
    {
        var count = Count;
        var range = new Range32(BoneCount, bones);
        if (range.End > _boneBuffer.Length) EnsureBoneCapacity(range.End);
        if (count >= _slotRanges.Length) EnsureSlotCapacity(count);
        BoneCount += bones;
        ++Count;
        _slotRanges[count] = range;
        return _boneBuffer.Slice(range);
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
        var dst = WriteSlot(length).Ptr;
        MatrixMath.MultiplyAffine(ref *++dst, in ctx.GetInverseBindPose(0), in globals[0]);
        for (var i = 1; i < length; ++i, ++dst)
        {
            var p = ctx.GetParentIndices(i);
            MatrixMath.MultiplyAffine(ref globals[i], in globals[p]);
            MatrixMath.MultiplyAffine(ref *dst, in ctx.GetInverseBindPose(i), in globals[i]);
        }
    }
    
    private void EnsureBoneCapacity(int length)
    {
        if (_boneBuffer.Length >= length + 1) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_boneBuffer.Length, length + 1);
        _boneBuffer.Resize(newSize, false);
        Console.WriteLine("BoneBuffer buffer resize");
    }

    private void EnsureSlotCapacity(int length)
    {
        if (_slotRanges.Length >= length + 1) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_slotRanges.Length, length + 1);
        Array.Resize(ref _slotRanges, newSize);
        Console.WriteLine("SlotRanges array resize");
    }

    //
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