using System.Diagnostics;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class DriverResourceStore<THandler> where THandler : unmanaged, IResourceHandle, IEquatable<THandler>
{
    private const int HardLimit = 10_000;

    private readonly record struct StoreEntry(THandler Value, ushort Gen, bool Alive);

    private StoreEntry[] _entries = new StoreEntry[16];
    private readonly Stack<int> _free = new();
    private readonly GraphicsBackend _backend = GraphicsBackend.Unkown;
    private readonly ResourceKind _kind = ResourceKind.Invalid;

    private int _idx = 0;

    public DriverResourceStore(GraphicsBackend backend, ResourceKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)backend, (int)GraphicsBackend.Unkown);
        ArgumentOutOfRangeException.ThrowIfEqual((int)kind, (int)ResourceKind.Invalid);
        _backend = backend;
        _kind =  kind;
    }

    public GfxHandle Add(THandler value)
    {
        Debug.Assert(!value.Equals(default));
        
        int idx = _free.Count > 0 ? _free.Pop() : Allocate();
        var prev = _entries[idx];
        var gen =  (ushort)(prev.Gen + 1);
        _entries[idx] = new StoreEntry(value, gen, true);
        return new GfxHandle((uint)idx, gen, _kind);
    }

    public THandler Get(GfxHandle handler)
    {
        Debug.Assert(handler != default);
        ref var e = ref _entries[(int)handler.Slot];
        if (!e.Alive || e.Gen != handler.Gen)
            GraphicsException.ThrowInvalidState("Handler is not a valid state");
        return e.Value;
    }

    public bool IsAlive(GfxHandle handler) =>
        handler != default &&
        _entries[(int)handler.Slot].Alive &&
        _entries[(int)handler.Slot].Gen == handler.Gen;

    public void Destroy(GfxHandle handler)
    {
        Debug.Assert(handler.Kind != ResourceKind.Invalid);

        ref var entry = ref _entries[(int)handler.Slot];
        if (handler == default || !entry.Alive || entry.Gen != handler.Gen) return;
        _entries[(int)handler.Slot] = default;
        _free.Push((int)handler.Slot);
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