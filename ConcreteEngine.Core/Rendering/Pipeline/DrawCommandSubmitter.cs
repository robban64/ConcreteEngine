#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Core.Rendering;

public class DrawCommandSubmitter
{
    // 10kb
    private const int DefaultBufferCapacity = 1024;
    private const int DefaultIndicesCapacity = 64;
    private const int MaxBufferCapacity = 1024 * 100;
    private const int MaxIndicesCapacity = 64 * 100;

    // key = DrawCommandId
    private readonly DispatchRegistry _registry = new();
    private IReadOnlyList<ICommandRenderer> _renderers;

    private Memory<byte> _buffer;
    private DrawCommandMetaIndex[] _indices;

    private int _submitIdx = 0;
    private int _iteratorIdx = 0;
    private int _stride = 0;

    public DrawCommandSubmitter()
    {
        _buffer = new byte[DefaultBufferCapacity];
        _indices = new DrawCommandMetaIndex[DefaultIndicesCapacity];
        _submitIdx = 0;
    }

    internal void Initialize(IReadOnlyList<ICommandRenderer> renderers) => _renderers = renderers;

    public void Register<T, TRenderer>(DrawCommandTag tag, params DrawCommandId[] cmdIds)
        where T : unmanaged, IDrawCommand
        where TRenderer : class, ICommandRenderer<T>
    {
        if (_renderers.Single(x => x.GetType() == typeof(TRenderer)) is not TRenderer renderer)
            throw new InvalidOperationException($"Renderer not found: {typeof(TRenderer).Name}");

        var size = Unsafe.SizeOf<T>();
        if (size > _stride) _stride = size;

        foreach (var id in cmdIds)
        {
            if (id == DrawCommandId.Invalid) throw new ArgumentException("Invalid command id", nameof(id));
            _registry.Register<T, TRenderer>(id, tag, renderer!);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommand
    {
        EnsureCapacity();
        var span = _buffer.Span;
        int size  = Unsafe.SizeOf<T>();
        int offset = _submitIdx * _stride;
        ref byte dest = ref span[offset];
        var slot = MemoryMarshal.CreateSpan(ref dest, size);
        MemoryMarshal.Write(slot, in cmd);   
        _indices[_submitIdx] = new DrawCommandMetaIndex(in meta, _submitIdx);
        _submitIdx++;
    }

    public void Prepare()
    {
        if (_submitIdx <= 2) return;
        _indices.AsSpan(0, _submitIdx).Sort();
    }


    public void DrainCommandQueue(RenderTargetId targetId)
    {
        for (int i = _iteratorIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref _indices[_iteratorIdx++];
            if (it.Meta.Target < targetId) continue;
            if (it.Meta.Target > targetId) break;

            _registry.Process(in it.Meta, _buffer.Span, it.Idx, _stride);
        }
    }

    public void Reset()
    {
        _submitIdx = 0;
        _iteratorIdx = 0;
    }

    private void EnsureCapacity()
    {
        var idx = _submitIdx + 1;

        var sizeInBytes = idx * _stride;
        if (sizeInBytes >= _buffer.Length)
        {
            int newSize = _buffer.Length + DefaultBufferCapacity;
            if (newSize > MaxBufferCapacity) throw new OutOfMemoryException("Command Buffer too big");
            var newArr = new byte[newSize];

            _buffer.Span.Slice(0, int.Min(_submitIdx * _stride, _buffer.Length)).CopyTo(newArr);
            _buffer = newArr;
        }

        if (idx >= _indices.Length)
        {
            int newSize = _indices.Length + DefaultBufferCapacity;
            if (newSize > MaxIndicesCapacity) throw new OutOfMemoryException("DrawCommandMeta Buffer too big");
            var newArr = new DrawCommandMetaIndex[newSize];
            _indices.AsSpan(0, int.Min(_submitIdx, _indices.Length)).CopyTo(newArr);
            _indices = newArr;
        }
    }


    private class DispatchRegistry
    {
        private readonly Dictionary<(DrawCommandId, DrawCommandTag), ProcessDelegate> _delegateRegistry = new();

        public void Process(in DrawCommandMeta meta, ReadOnlySpan<byte> buffer, int idx, int stride)
            => _delegateRegistry[(meta.Id, meta.Tag)](in meta, buffer, idx, stride);

        internal void Register<T, TRenderer>(DrawCommandId cmdId, DrawCommandTag tag, TRenderer handler)
            where T : unmanaged, IDrawCommand
            where TRenderer : class, ICommandRenderer<T>

        {
            // Handler
            /*
            var handlerType = typeof(ICommandRenderer<>).MakeGenericType(typeof(T));
            var method = handlerType.GetMethod(nameof(ICommandRenderer<T>.Handle));
            var action = typeof(DispatchToRendererDelegate<>).MakeGenericType(typeof(T));
            var del = (DispatchToRendererDelegate<T>)Delegate.CreateDelegate(action, handler, method!);
            */
            var method = typeof(ICommandRenderer<T>).GetMethod(nameof(ICommandRenderer<T>.Handle))!;
            var del = (DispatchToRendererDelegate<T>)Delegate.CreateDelegate(typeof(DispatchToRendererDelegate<T>),
                handler, method);
            int sizeT = Unsafe.SizeOf<T>();

            _delegateRegistry[(cmdId, tag)] = ProcessHandler;


            return;

            void ProcessHandler(in DrawCommandMeta meta, ReadOnlySpan<byte> buffer, int idx, int stride)
            {
                var slot = buffer.Slice(idx * stride, sizeT);
                var payload = MemoryMarshal.Read<T>(slot);
                del(in payload);
            }
        }
    }

    private delegate void DispatchToRendererDelegate<T>(in T payload) where T : unmanaged, IDrawCommand;

    private delegate void ProcessDelegate(in DrawCommandMeta meta, ReadOnlySpan<byte> buffer, int idx, int stride);


    private readonly struct DrawCommandMetaIndex(in DrawCommandMeta meta, int idx)
        : IComparable<DrawCommandMetaIndex>
    {
        public readonly DrawCommandMeta Meta = meta;
        public readonly int Idx = idx;

        private readonly ulong _sortKey =
            ((ulong)(byte)meta.Target << 56) |
            ((ulong)meta.View << 48) |
            ((ulong)meta.Queue << 40) |
            ((ulong)meta.DepthKey << 24) |
            ((ulong)meta.Layer << 16) |
            (ushort)idx;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
    }
}