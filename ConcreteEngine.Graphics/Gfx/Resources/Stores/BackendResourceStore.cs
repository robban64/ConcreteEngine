#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Gfx.Definitions;

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
        var gen = (ushort)(prev.Gen + 1);
        _records[idx] = new BkHandle<THandle>(value, gen, true);
        return GfxRefToken<TId>.From(new GfxHandle(idx, gen, Kind));
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
    }

    // Don't think this should be used, leaving it here for now
  /*  private GfxHandle Replace(in GfxHandle handle, THandle value)
    {
        Throwers.ThrowOnDefaultHandle(value);
        var oldValue = GetUntyped(handle);
        Throwers.IsUniqueHandleOrThrow(value.Value, oldValue.Value);

        var gen = (ushort)(handle.Gen + 1);
        _records[handle.Slot] = new BkHandle<THandle>(value, gen, true);
        return handle with { Gen = gen };
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