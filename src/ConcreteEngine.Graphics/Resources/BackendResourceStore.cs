using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendResourceStore : IDisposable
{
    GraphicsKind Kind { get; }
    NativeHandle GetNativeHandle(GfxHandle handle);
    void Remove(GfxHandle handle);

    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();
}

internal sealed class BackendResourceStore<THandle> : IBackendResourceStore where THandle : unmanaged, IGraphicsHandle
{
    private NativeArray<BkHandle> _handles;

    private readonly Stack<int> _free = new();
    public int Count { get; private set; }
    public GraphicsKind Kind { get; }

    public BackendResourceStore(int capacity, GraphicsKind kind)
    {
        Kind = kind;
        _handles = NativeArray.Allocate<BkHandle>(capacity);
    }

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _handles.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandle(GfxHandle gfxHandle)
    {
        Debug.Assert(gfxHandle.Kind == Kind);
        var handle = _handles[gfxHandle.Slot].Handle;
        return Unsafe.As<uint, THandle>(ref handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetNativeHandle(GfxHandle handle)
    {
        if (!handle.IsValid || handle.Kind != Kind) Throwers.InvalidOperation(nameof(handle));
        var record = _handles[handle.Slot];
        if (!record.IsValid()) Throwers.InvalidOperation(nameof(record));
        return new NativeHandle(record.Handle);
    }

    public GfxHandle Add(THandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value);
        var idx = AllocateNext();
        var newHandle = _handles[idx] = new BkHandle(handle.Value);
        GfxLog.LogBkStore(newHandle.Handle, idx, Kind.ToLogTopic(), LogAction.Add);
        return new GfxHandle(idx, 1, Kind);
    }

    public void Remove(GfxHandle handle)
    {
        if (!handle.IsValid || handle.Kind != Kind) Throwers.InvalidOperation(nameof(handle));
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)GraphicsKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var index = handle.Slot;
        var record = _handles[index];
        _handles[index] = default;

        Count = SlotHelper.FreeSlot(_free, index, Count);

        GfxLog.LogBkStore(record, index, Kind.ToLogTopic(), LogAction.Remove);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_handles[i].IsValid()) count++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _handles.Length) return;
        var newCap = CapacityUtils.CapacityGrowthToFit(_handles.Length, capacity);
        if (newCap > GfxLimits.StoreLimit)
            Throwers.BufferOverflow(typeof(BackendResourceStore<THandle>).Name, newCap, GfxLimits.StoreLimit);

        _handles.Resize(newCap, true);
    }

    private int AllocateNext()
    {
        var index = SlotHelper.NextSlot(_free, Count);
        if (index >= 0) return index;

        if (Count >= Capacity) EnsureCapacity(1);
        return Count++;
    }


    public void Dispose() => _handles.Dispose();
}