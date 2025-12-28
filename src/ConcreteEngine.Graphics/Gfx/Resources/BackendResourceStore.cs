using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal interface IBackendResourceStore
{
    GraphicsHandleKind Kind { get; }
    NativeHandle GetNativeHandle(in GfxHandle handle);
    void Remove(in GfxHandle handle);

    int Count { get; }
    int FreeCount { get; }
    int Capacity { get; }

    int GetAliveCount();
}

internal sealed class BackendResourceStore<TId, THandle> : IBackendResourceStore
    where THandle : unmanaged, IResourceHandle where TId : unmanaged, IResourceId
{
    private int _idx = 0;
    private BkHandle[] _records;
    private readonly Stack<int> _free = new();

    public GraphicsHandleKind Kind { get; }

    public int Count => _idx;
    public int FreeCount => _free.Count;
    public int Capacity => _records.Length;

    public BackendResourceStore(int capacity)
    {
        Kind = TId.Kind;
        _records = new BkHandle[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandle(GfxRefToken<TId> refToken)
    {
        var handle = _records[refToken.Slot].Handle;
        return Unsafe.As<uint, THandle>(ref handle);
    }

    public NativeHandle GetNativeHandle(in GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);

        var record = _records[handle.Slot];
        BkThrower.IsValidRecordOrThrow(record, handle);
        return new NativeHandle(record.Handle);
    }

    public GfxRefToken<TId> Add(THandle handle)
    {
        BkThrower.ThrowOnDefaultHandle(handle.Value);
        var idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var newHandle = _records[idx] = new BkHandle(handle.Value, true);
        GfxLog.LogBkStore(newHandle.Handle, idx, Kind.ToLogTopic(), LogAction.Add);
        return new GfxRefToken<TId>(idx, 1);
    }


    public void Remove(in GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)GraphicsHandleKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var record = _records[handle.Slot];
        _records[handle.Slot] = default;
        _free.Push(handle.Slot);
        GfxLog.LogBkStore(record, handle.Slot, Kind.ToLogTopic(), LogAction.Remove);
    }

    private int Allocate()
    {
        var len = _records.Length;
        if (_idx == len)
        {
            var newCap = Arrays.CapacityGrowthSafe(len, len + 1);
            Console.WriteLine("Backend store resize");

            if (newCap > GfxLimits.StoreLimit)
                throw new InvalidOperationException("Store limit exceeded");

            Array.Resize(ref _records, newCap);
        }

        return _idx++;
    }

    public int GetAliveCount()
    {
        var span = _records.AsSpan(0, _idx);
        var count = 0;
        foreach (var record in span)
        {
            if (record.IsValid) count++;
        }

        return count;
    }
}

internal static class BkThrower
{
    [DoesNotReturn]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalid(string name) => throw new InvalidOperationException(nameof(name));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidGfxHandleOrThrow(GfxHandle handle, GraphicsHandleKind kind)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowOnDefaultHandle(uint handle)
    {
        if (handle == 0) ThrowInvalid(nameof(handle));
    }
}