#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal interface IBackendResourceStore
{
    ResourceKind Kind { get; }

    NativeHandle GetNativeHandle(in GfxHandle handle);
    void Remove(in GfxHandle handle);
    bool IsValid(in GfxHandle handle);
}

internal interface IBackendReadResourceStore<out THandle>
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    THandle GetUntyped(in GfxHandle handle);
    //GfxHandle Replace(in GfxHandle handle, THandle value);
}

internal sealed class BackendResourceStore<TId, THandle> : IBackendResourceStore, IBackendReadResourceStore<THandle>
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle> where TId : unmanaged, IResourceId
{
    private int _idx = 0;
    private BkHandle<THandle>[] _records = new BkHandle<THandle>[16];
    private readonly Stack<int> _free = new();

    public ResourceKind Kind { get; }
    public GraphicsBackend Backend => GraphicsBackend.OpenGl;

    public BackendResourceStore(ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        Kind = kind;
    }

    public THandle GetHandle(GfxRefToken<TId> refToken) => _records[refToken.Handle.Slot].Handle;

    public NativeHandle GetNativeHandle(in GfxHandle handle) => NativeHandle.From(GetUntyped(in handle));

    public THandle GetUntyped(in GfxHandle handle)
    {
        Throwers.IsValidGfxHandleOrThrow(handle, Kind);
        ref readonly var record = ref _records[handle.Slot];
        Throwers.IsValidRecordOrThrow(record, handle);
        return record.Handle;
    }

    public bool IsValid(in GfxHandle handle) => _records[handle.Slot].IsValid;


    public GfxRefToken<TId> Add(THandle value)
    {
        Throwers.ThrowOnDefaultHandle(value);
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var prev = _records[idx];
        var newHandle = new BkHandle<THandle>(value, (ushort)(prev.Gen + 1), true);
        _records[idx] = newHandle;
        
        UpdateMetrics();
        GfxDebugMetrics.GetStoreMetrics<TId>().LastAddedBackend =
            BackendStoreEvent.Create(newHandle.Handle.Value, newHandle.Gen, true);


        return GfxRefToken<TId>.From(new GfxHandle(idx, newHandle.Gen, Kind));
    }


    public void Remove(in GfxHandle handle)
    {
        Throwers.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var idx = handle.Slot;
        var record = _records[idx];
        Throwers.IsValidRecordOrThrow(record, handle);

        _records[handle.Slot] = default;
        _free.Push(handle.Slot);

        UpdateMetrics();
        GfxDebugMetrics.GetStoreMetrics<TId>().LastRemovedBackend =
            BackendStoreEvent.Create(record.Handle.Value, handle.Gen, false);
    }

    public GfxRefToken<TId> Replace(GfxRefToken<TId> refToken, THandle value)
    {
        var handle = refToken.Handle;
        Throwers.ThrowOnDefaultHandle(value);
        var oldValue = GetUntyped(handle);
        Throwers.IsUniqueHandleOrThrow(value.Value, oldValue.Value);

        var result = new BkHandle<THandle>(value, (ushort)(handle.Gen + 1), true);
        _records[handle.Slot] = result;

        UpdateMetrics();
        GfxDebugMetrics.GetStoreMetrics<TId>().LastReplacedBackend =
            BackendStoreEvent.Create(result.Handle.Value, result.Gen, true);


        return GfxRefToken<TId>.From(handle with { Gen = result.Gen });
    }

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

    private void UpdateMetrics()
    {
        GfxDebugMetrics.GetStoreMetrics<TId>().BackendStoreCount = GetAliveCount();
        GfxDebugMetrics.GetStoreMetrics<TId>().BackendStoreFree = _free.Count;
    }

    private int GetAliveCount()
    {
        var span = _records.AsSpan(0, _idx);
        var count = 0;
        foreach (var record in span)
        {
            if (record.IsValid) count++;
        }

        return count;
    }

    private static class Throwers
    {
        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalid(string name) => throw new InvalidOperationException(nameof(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsValidGfxHandleOrThrow(GfxHandle handle, ResourceKind kind)
        {
            var isValid = handle.IsValid && handle.Kind == kind;
            if (!isValid) ThrowInvalid(nameof(handle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsValidRecordOrThrow(BkHandle<THandle> e, GfxHandle handle)
        {
            var isValid = e.IsValid && e.Gen == handle.Gen;
            if (!isValid) ThrowInvalid(nameof(e));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsUniqueHandleOrThrow(uint h1, uint h2)
        {
            if (h1 == h2) ThrowInvalid(nameof(h1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowOnDefaultHandle(THandle handle)
        {
            if (handle.Value == 0) ThrowInvalid(nameof(handle));
        }
    }
}