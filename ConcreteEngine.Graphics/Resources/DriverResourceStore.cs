using System.Diagnostics;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal interface IDriverResourceStore
{
    ResourceKind Kind { get; }
    void Remove(in GfxHandle handle);
    bool IsAlive(in GfxHandle handle);
    void NotifyReplace(in GfxHandle gfxHandle, BackendStoreRecreated callback);
}

internal interface IDriverReadResourceStore<THandle> where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    THandle Get(in GfxHandle handle);
    THandle GetForDelete(in GfxHandle handle);

    GfxHandle Replace(in GfxHandle handle, THandle value);
}

internal delegate void BackendStoreRecreated(in GfxHandle handle);

internal sealed class DriverResourceStore<THandle> : IDriverResourceStore, IDriverReadResourceStore<THandle>
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly record struct StoreRecord(THandle Current, ushort Gen, bool Alive)
    {
        public bool IsValidRecord() => Gen > 0 && Alive;
    }

    // sanity check
    private const int HardLimit = 10_000;

    private StoreRecord[] _entries = new StoreRecord[16];

    private readonly Stack<int> _free = new();

    private readonly GraphicsBackend _backend = GraphicsBackend.Unkown;
    private readonly ResourceKind _kind = ResourceKind.Invalid;

    private int _idx = 0;

    public ResourceKind Kind => _kind;

    public DriverResourceStore(GraphicsBackend backend, ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)backend, (int)GraphicsBackend.Unkown);
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        _backend = backend;
        _kind = kind;
    }

    public bool IsAlive(in GfxHandle handle) => _entries[(int)handle.Slot].IsValidRecord();

    public THandle Get(in GfxHandle handle)
    {
        Debug.Assert(handle.IsValid);
        ref readonly var e = ref _entries[(int)handle.Slot];
        if (!e.IsValidRecord() || e.Gen != handle.Gen)
            GraphicsException.ThrowInvalidState("Handler is not a valid state");
        return e.Current;
    }

    public THandle GetForDelete(in GfxHandle handle)
    {
        Debug.Assert(handle.IsValid);
        ref readonly var e = ref _entries[(int)handle.Slot];
        if (!e.IsValidRecord() || handle.Gen != e.Gen - 1)
            GraphicsException.ThrowInvalidState("Handler is not a valid state");
        return e.Current;
    }


    public GfxHandle Add(THandle value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var prev = _entries[idx];
        var gen = (ushort)(prev.Gen + 1);
        _entries[idx] = new StoreRecord(value, gen, true);
        return new GfxHandle((uint)idx, gen, _kind);
    }

    public GfxHandle Replace(in GfxHandle handle, THandle value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        var oldValue = Get(handle);
        if (value.Equals(oldValue))
            throw new GraphicsException("Trying to replace handler with same handler");

        var newGen = (ushort)(handle.Gen + 1);
        _entries[(int)handle.Slot] = new StoreRecord(value, newGen, true);
        return handle with { Gen = newGen };
    }


    public void NotifyReplace(in GfxHandle gfxHandle, BackendStoreRecreated callback)
    {
        var a = Get(gfxHandle);

        var record = _entries[(int)gfxHandle.Slot];
        _entries[(int)gfxHandle.Slot] = record with { IsPending = true };
        /* ref readonly var record = ref _entries[(int)gfxHandle.Slot];
         if (!_pending.TryAdd(gfxHandle.Slot, (record.Current, default)))
             throw new GraphicsException($"NotifyReplace: {gfxHandle} is already flagged as replaced. Duplication");
             */
    }


    public void Remove(in GfxHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        var idx = (int)handle.Slot;
        var entry = _entries[idx];
        if (!entry.IsValidRecord() || entry.Gen != handle.Gen)
            GraphicsException.ThrowInvalidState("Handler is not a valid state");

        _entries[(int)handle.Slot] = default;
        _free.Push((int)handle.Slot);
    }


    private int Allocate()
    {
        if (_idx == _entries.Length)
        {
            Debug.Assert(_idx < HardLimit);

            var newCap = _entries.Length * 2;
            Array.Resize(ref _entries, newCap);
        }

        return _idx++;
    }
}
