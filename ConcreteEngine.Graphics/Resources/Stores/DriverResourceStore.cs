using System.Diagnostics;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

interface IDriverResourceStore
{
    void Remove(GfxHandle handle);
    bool IsAlive(GfxHandle handle);
}
internal sealed class DriverResourceStore<THandle> : IDriverResourceStore where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly record struct StoreRecord(THandle Value, ushort Gen, bool Alive)
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

    public DriverResourceStore(GraphicsBackend backend, ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)backend, (int)GraphicsBackend.Unkown);
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        _backend = backend;
        _kind = kind;
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

    public GfxHandle Replace(GfxHandle handle, THandle value)
    {
        var oldValue = Get(handle);
        if (value.Equals(oldValue)) 
            throw new GraphicsException("Trying to replace handler with same handler");
        var newGen = (ushort)(handle.Gen + 1);
        _entries[(int)handle.Slot] = new StoreRecord(value, newGen, true);
        return handle with { Gen = newGen };
    }

    public THandle Get(GfxHandle handle)
    {
        Debug.Assert(handle.IsValid);
        ref readonly var e = ref _entries[(int)handle.Slot];
        if (!e.IsValidRecord() || e.Gen != handle.Gen)
            GraphicsException.ThrowInvalidState("Handler is not a valid state");
        return e.Value;
    }

    public bool IsAlive(GfxHandle handle) => _entries[(int)handle.Slot].IsValidRecord();

    public void Remove(GfxHandle handle)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)handle.Kind, (int)ResourceKind.Invalid);
        ArgumentOutOfRangeException.ThrowIfEqual(handle.Gen, 0);

        ref var entry = ref _entries[(int)handle.Slot];
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