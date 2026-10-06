using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Animations;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderLimits;

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
            UpdateSkinned(animation);
            WriteSkeleton(animation.Rig);
        }

    }

    public void Dispose()
    {
        _scratchGlobals.Dispose();
        _boneBuffer.Dispose();
    }

    private static int FilterEntities(int slot, ReadOnlySpan<RenderEntity> entities)
    {
        var count = 0;
        foreach (var query in RenderWorld.Query.New<SkinningLink>().SparseEntityFilter(entities, RenderWorld.Meta.VisibleSet))
        {
            query.Component.AnimationSlot = (ushort)slot;
            ++count;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<Matrix4x4> NextSkinningView(int bones)
    {
        var count = Count++;
        var range = new Range32(BoneCount, bones);
        if (range.End > _boneBuffer.Length) EnsureBoneCapacity(range.End);
        if (count >= _slotRanges.Length) EnsureSlotCapacity(count);
        BoneCount += bones;
        _slotRanges[count] = range;
        return _boneBuffer.AsSpan(range.Offset, range.Length);
    }

    private void UpdateSkinned(AnimationInstance animation)
    {
        var time = (float)animation.Time;

        var rig = animation.Rig;
        var length = animation.Rig.BoneCount;
        var currentTrack = animation.GetActiveClip().BoneTracks;
        
        var dst = _scratchGlobals.AsSpan(0, length);

        for (int i = 0; i < length; ++i)
        {
            var track = currentTrack[i];
            if (track.IsEmpty)
            {
                dst[i] = rig.GetBindPose(i);
                continue;
            }

            var posFactor = GetIndexFactor(time, track.PositionTimesPtr, track.PosCount, out var posIndex);
            var rotFactor = GetIndexFactor(time, track.RotationTimesPtr, track.RotCount, out var rotIndex);

            var pos = GetPosition(posIndex, posFactor, track.PositionPtr);
            var rot = GetRotation(rotIndex, rotFactor, track.RotationPtr);

            MatrixMath.CreateFixedSizeModelMatrix(pos, in rot, out dst[i]);
        }
    }

    private void WriteSkeleton(ModelRig rig)
    {
        var length = rig.BoneCount;

        var indices = rig.ParentIndices().Slice(0, length);
        var inverseBindPoses = rig.InverseBindPose().Slice(0, length);

        var dst = NextSkinningView(length);
        var globals = _scratchGlobals;

        if((uint)length > (uint)globals.Length || (uint)length > (uint)dst.Length) Throwers.InvalidOperation();

        Matrix4x4 parentTransform;
        for (var i = 1; i < length; ++i)
        {
            parentTransform = globals[indices[i]];
            MatrixMath.MultiplyAffine(ref globals[i], in parentTransform);
        }

        for (var i = 0; i < length; ++i)
        {
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
    private static Vector3 GetPosition(int posIndex, float posFactor, Vector3* positions)
    {
        if (posIndex > 0) return Vector3.Lerp(positions[posIndex], positions[posIndex + 1], posFactor);
        return *positions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion GetRotation(int rotIndex, float rotFactor, Quaternion* rotation)
    {
        if (rotIndex > 0) return Quaternion.Slerp(rotation[rotIndex], rotation[rotIndex + 1], rotFactor);
        return *rotation;
    }

    private static float GetIndexFactor(float time, float* times, int length, out int index)
    {
        if (length == 1)
        {
            index = -1;
            return 0;
        }

        var idx = FindIndex(new ReadOnlySpan<float>(times, length), time);
        var i0 = times[idx];
        var i1 = times[idx + 1];

        index = idx;
        return (time - i0) / (i1 - i0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindIndex(ReadOnlySpan<float> keys, float time)
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