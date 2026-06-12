using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using static ConcreteEngine.Renderer.RenderLimits;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class SkinningBuffer : IDisposable
{
    private const int DefaultBoneBufferCap = BoneCapacity * 64;

    public int Count { get; private set; }

    private NativeArray<Matrix4x4> _matrices;

    internal SkinningBuffer()
    {
        _matrices = NativeArray.AlignedAllocate<Matrix4x4>(DefaultBoneBufferCap, alignment: 16);
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort NextSlot()
    {
        return (ushort)(++Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Matrix4x4> GetWriteView(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfZero(slot);
        return _matrices.Slice((slot - 1) * BoneCapacity, BoneCapacity);
    }

    internal NativeView<Matrix4x4> DrainBuffer()
    {
        var len = Count * BoneCapacity;
        if (_matrices.Length == 0) return NativeView<Matrix4x4>.MakeNull();
        if ((uint)len > (uint)_matrices.Length) Throwers.InvalidOperation();

        return _matrices.Slice(0, Count * BoneCapacity);
    }

    internal void Reset() => Count = 0;

    public void EnsureCapacity(int requiredLength)
    {
        var len = requiredLength * BoneCapacity;
        if (_matrices.Length >= len) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_matrices.Length, len);
        _matrices.Resize(newSize, false);
        Console.WriteLine("BoneBuffer buffer resize");
    }

    public void Dispose() => _matrices.Dispose();
}