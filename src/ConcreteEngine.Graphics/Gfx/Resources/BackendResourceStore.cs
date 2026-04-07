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

internal interface IBackendResourceStore
{
    GraphicsKind Kind { get; }
    NativeHandle GetNativeHandle(GfxHandle handle);
    void Remove(GfxHandle handle);

    int Count { get; }
    int FreeCount { get; }
    int Length { get; }

    int GetAliveCount();
}

internal sealed class BackendResourceStore<THandle> : IBackendResourceStore, IDisposable
    where THandle : unmanaged, IGraphicsHandle
{
    private int _count;
    private NativeArray<BkHandle> _records;
    
    private readonly Stack<int> _free = new();

    public GraphicsKind Kind { get; }

    public int Count => _count;
    public int FreeCount => _free.Count;
    public int Length => _records.Length;

    public BackendResourceStore(int capacity, GraphicsKind kind)
    {
        Kind = kind;
        _records = NativeArray.Allocate<BkHandle>(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandle(GfxHandle gfxHandle)
    {
        Debug.Assert(gfxHandle.Kind == Kind);
        var handle = _records[gfxHandle.Slot].Handle;
        return Unsafe.As<uint, THandle>(ref handle);
    }

    public NativeHandle GetNativeHandle(GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);

        var record = _records[handle.Slot];
        BkThrower.IsValidRecordOrThrow(record, handle);
        return new NativeHandle(record.Handle);
    }

    public GfxHandle Add(THandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfZero(handle.Value);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var newHandle = _records[idx] = new BkHandle(handle.Value, true);
        GfxLog.LogBkStore(newHandle.Handle, idx, Kind.ToLogTopic(), LogAction.Add);
        return new GfxHandle(idx, 1, Kind);
    }

    public void Remove(GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)GraphicsKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var record = _records[handle.Slot];
        _records[handle.Slot] = default;
        _free.Push(handle.Slot);
        GfxLog.LogBkStore(record, handle.Slot, Kind.ToLogTopic(), LogAction.Remove);
    }

    public int GetAliveCount()
    {
        var count = 0;
        for (var i = 0; i < _count; i++)
        {
            if (_records[i].IsValid) count++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity <= _records.Length) return;
        var newCap = Arrays.CapacityGrowthSafe(_records.Length, IntMath.AlignUp(64, capacity));
        if (newCap > GfxLimits.StoreLimit)
            throw new InvalidOperationException("Store limit exceeded");

        _records.Resize(newCap, true);
    }

    private int Allocate()
    {
        var len = _records.Length;
        if (_count == len)
            EnsureCapacity(len + 1);

        return _count++;
    }

    public void Dispose() => _records.Dispose();
}

file static class BkThrower
{
    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalid(string name) => throw new InvalidOperationException(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidGfxHandleOrThrow(GfxHandle handle, GraphicsKind kind)
    {
        var isValid = handle.IsValid && handle.Kind == kind;
        if (!isValid) ThrowInvalid(nameof(handle));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidRecordOrThrow(uint handle, GfxHandle gfxHandle)
    {
        var isValid = handle > 0 && gfxHandle.IsValid;
        if (!isValid) ThrowInvalid(nameof(handle));
    }
}