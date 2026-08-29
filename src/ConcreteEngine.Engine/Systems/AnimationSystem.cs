using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using static ConcreteEngine.Engine.Render.RenderLimits;

namespace ConcreteEngine.Engine.Systems;

internal sealed unsafe class AnimationSystem : IDisposable
{
    private const int DefaultCapacity = 64;
    private const int DefaultBoneBufferCap = BoneCapacity * 64;

    public int Count { get; private set; }
    public int BoneCount { get; private set; }

    private Range32[] _slotRanges;
    private NativeArray<Matrix4x4> _boneBuffer;

    private NativeArray<Matrix4x4> _scratchGlobals;

    private readonly AnimationManager _animations;

    private readonly List<Id16<AnimationInstance>> _animationIds = new(32);

    internal AnimationSystem(AnimationManager animations)
    {
        _scratchGlobals = NativeArray.AlignedAllocate<Matrix4x4>(BoneCapacity, alignment: 64, false);
        _boneBuffer = NativeArray.AlignedAllocate<Matrix4x4>(DefaultBoneBufferCap, alignment: 64, false);
        _slotRanges = new Range32[DefaultCapacity];

        _animations = animations;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Range32 GetSlotRange(int slot) => _slotRanges[slot];

    public NativeView<SkinningUniform> GetUniforms()
    {
        if (BoneCount == 0) return NativeView<SkinningUniform>.MakeNull();
        if ((uint)BoneCount >= (uint)_boneBuffer.Length) Throwers.InvalidOperation();
        return new NativeView<SkinningUniform>((SkinningUniform*)_boneBuffer.Ptr, BoneCount);
    }

    public void ResetFrame()
    {
        Count = 0;
        BoneCount = 0;
    }

    public void Simulate(double dt)
    {
        foreach (var it in _animations) it.AdvanceTime(dt);
    }

    public void Execute(double alpha)
    {
        _animationIds.Clear();

        foreach (var animation in _animations)
        {
            var count = FilterEntities(_animationIds.Count + 1, animation.GetEntitySpan());
            if (count == 0) continue;

            animation.Interpolate(alpha);
            _animationIds.Add(animation.Id);
        }

        foreach (var id in _animationIds.AsSpan())
        {
            var animation = _animations.Get(id);
            var time = (float)animation.Time;
            UpdateSkinned(animation.Rig, animation.ActiveClip, time);
            WriteSkeleton(animation.Rig);
        }
    }


    public void Dispose()
    {
        _scratchGlobals.Dispose();
        _boneBuffer.Dispose();
    }

    private static int FilterEntities(int slot, ReadOnlySpan<RenderEntityId> entities)
    {
        var count = 0;
        foreach (var query in RenderEcs.Store<SkinningLink>().SparseQuery(entities))
        {
            if (!RenderEcs.Core.IsVisible(query.Entity)) continue;
            query.Component.AnimationSlot = (ushort)slot;
            ++count;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NativeView<Matrix4x4> NextSkinningView(int bones)
    {
        var count = Count++;
        var range = new Range32(BoneCount, bones);
        if (range.End > _boneBuffer.Length) EnsureBoneCapacity(range.End);
        if (count >= _slotRanges.Length) EnsureSlotCapacity(count);
        BoneCount += bones;
        _slotRanges[count] = range;
        return _boneBuffer.Slice(range);
    }

    private void UpdateSkinned(ModelRig rig, int clipIndex, float time)
    {
        var globals = _scratchGlobals.Ptr;
        var track = rig.GetClipView(clipIndex).BoneTracks;
        var length = rig.BoneCount;
        for (var i = 0; i < length; ++i, ++track, ++globals)
        {
            if (track->IsEmpty)
            {
                *globals = rig.GetBindPose(i);
                continue;
            }

            var posFactor = GetIndexFactor(time, track->PositionTimes, out var posIndex);
            var rotFactor = GetIndexFactor(time, track->RotationTimes, out var rotIndex);

            var pos = GetPosition(posIndex, posFactor, track->Positions);
            var rot = GetRotation(rotIndex, rotFactor, track->Rotations);

            MatrixMath.CreateFixedSizeModelMatrix(in pos, in rot, out *globals);
        }
    }

    private void WriteSkeleton(ModelRig rig)
    {
        var length = rig.BoneCount;
        var indices = rig.ParentIndicesArray.AsSpan(0, length);
        var inverseBindPoses = rig.InverseBindPoseArray.AsSpan(0, length);
        var globals = _scratchGlobals.Ptr;
        var dst = NextSkinningView(length);

        MatrixMath.MultiplyAffine(ref dst[0], in inverseBindPoses[0], in globals[0]);
        for (var i = 1; i < indices.Length; ++i)
        {
            var p = indices[i];
            MatrixMath.MultiplyAffine(ref globals[i], in globals[p]);
            MatrixMath.MultiplyAffine(ref dst[i], in inverseBindPoses[i], in globals[i]);
        }
    }


    private void EnsureBoneCapacity(int length)
    {
        if (_boneBuffer.Length >= length + 1) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_boneBuffer.Length, length + 1);
        _boneBuffer.ReAlloc(newSize, false);
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