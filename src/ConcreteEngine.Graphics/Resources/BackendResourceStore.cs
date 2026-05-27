using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendResourceStore : IDisposable
{
    GraphicsKind Kind { get; }
    NativeHandle GetSafe(GfxHandle handle);
    void Remove(GfxHandle handle);

    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();
}

internal sealed class BackendResourceStore : IBackendResourceStore
{
    private NativeArray<NativeHandle> _handles;

    private readonly Stack<int> _free = new();
    public int Count { get; private set; }
    public GraphicsKind Kind { get; }

    public BackendResourceStore(int capacity, GraphicsKind kind)
    {
        Kind = kind;
        _handles = NativeArray.Allocate<NativeHandle>(capacity);
    }

    public int ActiveCount => Count - _free.Count;
    public int FreeCount => _free.Count;
    public int Capacity => _handles.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle Get(GfxHandle gfxHandle)
    {
        Debug.Assert(gfxHandle.Kind == Kind);
        return _handles[gfxHandle.Slot];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetSafe(GfxHandle handle)
    {
        if (!handle.IsValid || handle.Kind != Kind) Throwers.InvalidOperation(nameof(handle));
        var record = _handles[handle.Slot];
        if (!record.IsValid()) Throwers.InvalidOperation(nameof(record));
        return record;
    }

    public GfxHandle Add(NativeHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value);
        var idx = AllocateNext();
        _handles[idx] = handle;
        GfxLog.LogBkStore(handle, idx, Kind.ToLogTopic(), LogAction.Add);
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
            Throwers.BufferOverflow(nameof(BackendResourceStore), newCap, GfxLimits.StoreLimit);

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