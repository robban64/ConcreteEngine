using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

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

    public NativeHandle GetNativeHandle(GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);

        var record = _handles[handle.Slot];
        BkThrower.IsValidRecordOrThrow(record, handle);
        return new NativeHandle(record.Handle);
    }

    public GfxHandle Add(THandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var newHandle = _handles[idx] = new BkHandle(handle.Value, true);
        GfxLog.LogBkStore(newHandle.Handle, idx, Kind.ToLogTopic(), LogAction.Add);
        return new GfxHandle(idx, 1, Kind);
    }

    public void Remove(GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)GraphicsKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var index = handle.Slot;
        var record = _handles[index];
        _handles[index] = default;

        if (index == Count - 1) Count--;
        else _free.Push(index);
        if (ActiveCount == 0 && Count > 0)
        {
            _free.Clear();
            Count = 0;
        }

        GfxLog.LogBkStore(record, index, Kind.ToLogTopic(), LogAction.Remove);
    }

    public int GetAliveCount()
    {
        var count = 0;
        var length = Count;
        for (var i = 0; i < length; i++)
        {
            if (_handles[i].IsValid) count++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _handles.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_handles.Length, IntMath.AlignUp(64, capacity));
        if (newCap > GfxLimits.StoreLimit)
            throw new InvalidOperationException("Store limit exceeded");

        _handles.Resize(newCap, true);
    }

    private int Allocate()
    {
        var len = _handles.Length;
        if (Count == len)
            EnsureCapacity(len + 1);

        return Count++;
    }

    public void Dispose() => _handles.Dispose();
}

file static class BkThrower
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidGfxHandleOrThrow(GfxHandle handle, GraphicsKind kind)
    {
        var isValid = handle.IsValid && handle.Kind == kind;
        if (!isValid) throw new InvalidOperationException(nameof(handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidRecordOrThrow(uint handle, GfxHandle gfxHandle)
    {
        var isValid = handle > 0 && gfxHandle.IsValid;
        if (!isValid) throw new InvalidOperationException(nameof(handle));
    }
}