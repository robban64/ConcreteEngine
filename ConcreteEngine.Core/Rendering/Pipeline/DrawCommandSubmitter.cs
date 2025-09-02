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
        where T : struct, IDrawCommand
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
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : struct, IDrawCommand
    {
        int size = Unsafe.SizeOf<T>();
        EnsureCapacity();
        var slot = _buffer.Span.Slice(_submitIdx * _stride, size);
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
        while (_iteratorIdx < _submitIdx)
        {
            ref readonly var it = ref _indices[_iteratorIdx++];
            if (it.Meta.Target < targetId) continue;
            if (it.Meta.Target > targetId) break;

            _registry.Process(in it.Meta, _buffer, it.Idx, _stride);
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
        private readonly Dictionary<(DrawCommandId, DrawCommandTag), Delegate> _dispatchRegistry = new();
        private readonly Dictionary<DrawCommandId, ProcessDelegate> _commandRegistry = new();

        internal void Process(in DrawCommandMeta meta, Memory<byte> buffer, int idx, int stride)
        {
            var cmdHandler = _commandRegistry[meta.Id];
            cmdHandler(in meta, buffer, idx, stride);
        }

        internal void Register<T, TRenderer>(DrawCommandId cmdId, DrawCommandTag tag, TRenderer handler)
            where T : struct, IDrawCommand
            where TRenderer : class, ICommandRenderer<T>

        {
            // Handler
            var handlerType = typeof(ICommandRenderer<>).MakeGenericType(typeof(T));
            var method = handlerType.GetMethod(nameof(ICommandRenderer<T>.Handle));
            var action = typeof(DispatchToRendererDelegate<>).MakeGenericType(typeof(T));
            var del = (DispatchToRendererDelegate<T>)Delegate.CreateDelegate(action, handler, method!);
            _dispatchRegistry[(cmdId, tag)] = del;

            _commandRegistry[cmdId] = ProcessHandler;

            return;

            // Reader
            void ProcessHandler(in DrawCommandMeta meta, Memory<byte> buffer, int idx, int stride)
            {
                var structSize = Unsafe.SizeOf<T>();

                var slot = buffer.Span.Slice(idx * stride, structSize);
                var payload = MemoryMarshal.Read<T>(slot);
                ((DispatchToRendererDelegate<T>)_dispatchRegistry[(meta.Id, meta.Tag)])(in payload);
            }
        }
    }

    private delegate void DispatchToRendererDelegate<T>(in T payload) where T : struct, IDrawCommand;

    private delegate void ProcessDelegate(in DrawCommandMeta meta, Memory<byte> buffer, int idx, int stride);


    private readonly struct DrawCommandMetaIndex(in DrawCommandMeta meta, int idx)
        : IComparable<DrawCommandMetaIndex>
    {
        public readonly DrawCommandMeta Meta = meta;
        public readonly int Idx = idx;

        private readonly uint _sortKey = (uint)((byte)meta.Target << 16 | (meta.Layer << 8) | (ushort)idx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(DrawCommandMetaIndex other) => _sortKey.CompareTo(other._sortKey);
    }
}