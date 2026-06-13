using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using static ConcreteEngine.Renderer.RenderLimits;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class SkinningBuffer : IDisposable
{
    private const int DefaultCapacity = 64;
    private const int DefaultBoneBufferCap = BoneCapacity * 64;

    public int Count { get; private set; }
    private int _boneCount;
    
    private NativeArray<Matrix4x4> _matrices;
    private Range32[] _slotRanges;

    internal SkinningBuffer()
    {
        _matrices = NativeArray.AlignedAllocate<Matrix4x4>(DefaultBoneBufferCap, alignment: 16);
        _slotRanges = new Range32[DefaultCapacity];
        Count = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Range32 GetSlotRange(int slot) => _slotRanges[slot];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Matrix4x4> WriteSlot(int bones)
    {
        var count = Count;
        var range = new Range32(_boneCount, bones);
        if(range.End > _matrices.Length) EnsureBoneCapacity(range.End);
        if(count >= _slotRanges.Length) EnsureSlotCapacity(count);
        _boneCount += bones;
        ++Count;
        _slotRanges[count] = range;
        return _matrices.Slice(range);
    }
    
    internal NativeView<Matrix4x4> DrainBuffer()
    {
        if (_boneCount == 0) return NativeView<Matrix4x4>.MakeNull();
        if ((uint)_boneCount >= (uint)_matrices.Length) Throwers.InvalidOperation();
        return _matrices.Slice(0, _boneCount);
    }

    internal void Reset()
    {
        Count = 0;
        _boneCount = 0;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureBoneCapacity(int length)
    {
        if (_matrices.Length >= length + 1) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_matrices.Length, length + 1);
        _matrices.Resize(newSize, false);
        Console.WriteLine("BoneBuffer buffer resize");
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureSlotCapacity(int length)
    {
        if (_slotRanges.Length >= length + 1) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_slotRanges.Length, length + 1);
        Array.Resize(ref _slotRanges, newSize);
        Console.WriteLine("SlotRanges array resize");
    }

    public void Dispose() => _matrices.Dispose();
}