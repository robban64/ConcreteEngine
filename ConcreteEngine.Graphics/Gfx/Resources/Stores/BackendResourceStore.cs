#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal interface IBackendResourceStore
{
    ResourceKind Kind { get; }
    NativeHandle GetNativeHandle(in GfxHandle handle);
    void Remove(in GfxHandle handle);
}


internal sealed class BackendResourceStore<TId, THandle> : IBackendResourceStore
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle> where TId : unmanaged, IResourceId
{
    private int _idx = 0;
    private BkHandle<THandle>[] _records = new BkHandle<THandle>[32];
    private readonly Stack<int> _free = new();

    public ResourceKind Kind { get; }

    public int Count => _idx;
    public int FreeCount => _free.Count;
    public int Capacity => _records.Length;

    public BackendResourceStore(ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        Kind = kind;
    }

    public THandle GetHandle(GfxRefToken<TId> refToken) => _records[refToken.Handle.Slot].Handle;

    public NativeHandle GetNativeHandle(in GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);

        var record = _records[handle.Slot];
        BkThrower.IsValidRecordOrThrow(record, handle);
        return NativeHandle.From(record.Handle);
    }

    public GfxRefToken<TId> Add(THandle handle)
    {
        BkThrower.ThrowOnDefaultHandle(handle.Value);
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var newHandle = _records[idx] = new BkHandle<THandle>(handle, true);
        GfxLog.LogBkStore(newHandle, idx, Kind.ToLogTopic(), LogAction.Add);
        return new GfxRefToken<TId>(new GfxHandle(idx, 1, Kind));
    }


    public void Remove(in GfxHandle handle)
    {
        BkThrower.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var record = _records[handle.Slot];
        _records[handle.Slot] = default;
        _free.Push(handle.Slot);
        GfxLog.LogBkStore(record, handle.Slot, Kind.ToLogTopic(), LogAction.Remove);
    }

/*
    public GfxRefToken<TId> Replace(GfxRefToken<TId> refToken, THandle value)
    {
        Throwers.ThrowOnDefaultHandle(value);

        var handle = refToken.Handle;
        var prev = _records[handle.Slot];
        var newRecord = _records[handle.Slot] = new BkHandle<THandle>(value, true);

        GfxLog.LogBkStore(value, prev, TId.Kind.ToLogTopic(),  LogAction.Replace,0);
        GfxLog.LogBkStore(value, refToken, TId.Kind.ToLogTopic(),  LogAction.Replace, 1);

        return GfxRefToken<TId>.MakeBkRef(handle.Slot);
    }
*/
    private int Allocate()
    {
        var len = _records.Length;
        if (_idx == len)
        {
            var newCap = ArrayUtility.CapacityGrowthLinear(len, len * 2, step: 32);
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
    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalid(string name) => throw new InvalidOperationException(nameof(name));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsValidGfxHandleOrThrow(GfxHandle handle, ResourceKind kind)
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