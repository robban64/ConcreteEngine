using System.Diagnostics;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendResourceStore
{
    ResourceKind Kind { get; }

    NativeHandle GetNativeHandle(in GfxHandle handle);
    void Remove(in GfxHandle handle);
    bool IsAlive(in GfxHandle handle);
}

internal interface IBackendReadResourceStore<out THandle> where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    THandle Get(in GfxHandle handle);
    //GfxHandle Replace(in GfxHandle handle, THandle value);
}

internal sealed class BackendResourceStore<THandle> : IBackendResourceStore, IBackendReadResourceStore<THandle>
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly record struct StoreRecord(THandle Current, ushort Gen, bool Alive)
    {
        public bool IsValidRecord() => Gen > 0 && Alive;
    }

    // sanity check
    private const int HardLimit = 10_000;

    private int _idx = 0;
    private StoreRecord[] _entries = new StoreRecord[16];
    private readonly Stack<int> _free = new();


    public ResourceKind Kind { get; }
    public GraphicsBackend Backend => GraphicsBackend.OpenGL;

    public BackendResourceStore(ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        Kind = kind;
    }

    public bool IsAlive(in GfxHandle handle) => _entries[(int)handle.Slot].IsValidRecord();

    public NativeHandle GetNativeHandle(in GfxHandle handle) => NativeHandle.From(Get(in handle));

    public THandle Get(in GfxHandle handle)
    {
        Debug.Assert(handle.IsValid);
        ref readonly var e = ref _entries[(int)handle.Slot];
        if (!e.IsValidRecord() || e.Gen != handle.Gen)
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
        return new GfxHandle((uint)idx, gen, Kind);
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
    
    // Don't think this should be used, leaving it here for now
    public GfxHandle Replace(in GfxHandle handle, THandle value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        var oldValue = Get(handle);
        if (value.Handle == oldValue.Handle)
            throw new GraphicsException("Trying to replace handler with same handler");

        var gen = (ushort)(handle.Gen + 1);
        _entries[(int)handle.Slot] = new StoreRecord(value, gen, true);
        return handle with { Gen = gen };
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
