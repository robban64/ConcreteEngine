#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

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
    THandle Get(in GfxHandle handle);
    //GfxHandle Replace(in GfxHandle handle, THandle value);
}

internal sealed class BackendResourceStore<THandle> : IBackendResourceStore, IBackendReadResourceStore<THandle>
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly struct StoreRecord(THandle current, ushort gen, bool alive)
    {
        public readonly THandle Current = current;
        public readonly ushort Gen = gen;
        public readonly bool Alive = alive;
        public readonly bool IsValid = current.Handle > 0 && gen > 0 && alive;
    }

    // sanity check
    private const int HardLimit = 10_000;

    private int _idx = 0;
    private StoreRecord[] _records = new StoreRecord[16];
    private readonly Stack<int> _free = new();


    public ResourceKind Kind { get; }
    public GraphicsBackend Backend => GraphicsBackend.OpenGL;

    public BackendResourceStore(ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        Kind = kind;
    }

    public bool IsValid(in GfxHandle handle) => _records[(int)handle.Slot].IsValid;

    public NativeHandle GetNativeHandle(in GfxHandle handle) => NativeHandle.From(Get(in handle));

    public THandle Get(in GfxHandle handle)
    {
        Throwers.IsValidGfxHandleOrThrow(handle, Kind);
        ref readonly var record = ref _records[(int)handle.Slot];
        Throwers.IsValidRecordOrThrow(record, handle);
        return record.Current;
    }

    public THandle GetRef<TId>(GfxRefToken<TId> refToken) where TId : unmanaged, IResourceId
    {
        var handle = refToken.Handle;
        ref readonly var record = ref _records[(int)handle.Slot];
        Debug.Assert(handle.Kind == Kind && record.IsValid && record.Gen == handle.Gen);
        return record.Current;
    }

    public GfxHandle Add(THandle value)
    {
        Throwers.ThrowOnDefaultHandle(value);
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var prev = _records[idx];
        var gen = (ushort)(prev.Gen + 1);
        _records[idx] = new StoreRecord(value, gen, true);
        return new GfxHandle((uint)idx, gen, Kind);
    }


    public void Remove(in GfxHandle handle)
    {
        Throwers.IsValidGfxHandleOrThrow(handle, Kind);
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var idx = (int)handle.Slot;
        var record = _records[idx];
        Throwers.IsValidRecordOrThrow(record, handle);

        _records[(int)handle.Slot] = default;
        _free.Push((int)handle.Slot);
    }

    // Don't think this should be used, leaving it here for now
    public GfxHandle Replace(in GfxHandle handle, THandle value)
    {
        Throwers.ThrowOnDefaultHandle(value);
        var oldValue = Get(handle);
        Throwers.IsUniqueHandleOrThrow(value.Handle, oldValue.Handle);

        var gen = (ushort)(handle.Gen + 1);
        _records[(int)handle.Slot] = new StoreRecord(value, gen, true);
        return handle with { Gen = gen };
    }

    private int Allocate()
    {
        if (_idx == _records.Length)
        {
            Debug.Assert(_idx < HardLimit);

            var newCap = _records.Length * 2;
            Array.Resize(ref _records, newCap);
        }

        return _idx++;
    }

    private static class Throwers
    {
        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowInvalid(string name) => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsValidGfxHandleOrThrow(GfxHandle handle, ResourceKind kind)
        {
            var isValid = handle.IsValid && handle.Kind == kind;
            if (!isValid) ThrowInvalid(nameof(handle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsValidRecordOrThrow(StoreRecord e, GfxHandle handle)
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
            if (handle.Handle == 0) ThrowInvalid(nameof(handle));
        }
    }
}