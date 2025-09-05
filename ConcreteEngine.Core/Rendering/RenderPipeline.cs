#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface IRenderPipeline
{
    void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommand;

    void SubmitDrawBatch<T>(ReadOnlySpan<T> cmds, ReadOnlySpan<DrawCommandMeta> metas)
        where T : unmanaged, IDrawCommand;
}

internal sealed class RenderPipeline : IRenderPipeline
{
    // 10kb
    private const int DefaultBufferCapacity = 1024;
    private const int DefaultIndicesCapacity = 64;
    private const int MaxBufferCapacity = 1024 * 100;
    private const int MaxIndicesCapacity = 64 * 100;

    // key = DrawCommandId
    private readonly DispatchRegistry _registry;

    private Memory<byte> _buffer;
    private DrawCommandMetaIndex[] _indices;

    private int _submitIdx = 0;
    private int _iteratorIdx = 0;
    private int _stride = 0;

    public RenderPipeline()
    {
        _registry = new DispatchRegistry();
        _buffer = new byte[DefaultBufferCapacity];
        _indices = new DrawCommandMetaIndex[DefaultIndicesCapacity];
        _submitIdx = 0;
    }

    internal void Initialize() {}

    public void Register<T>(DrawCommandId commandId) where T : unmanaged, IDrawCommand
    {
        var size = Unsafe.SizeOf<T>();
        if (size > _stride) _stride = size;
        if (commandId == DrawCommandId.Invalid) throw new ArgumentException("Invalid command id", nameof(commandId));
        _registry.Register<T>(commandId);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommand
    {
        EnsureCapacity(1);
        var span = _buffer.Span;
        int size = Unsafe.SizeOf<T>();
        int offset = _submitIdx * _stride;
        ref byte dest = ref span[offset];
        var slot = MemoryMarshal.CreateSpan(ref dest, size);
        MemoryMarshal.Write(slot, in cmd);
        _indices[_submitIdx] = new DrawCommandMetaIndex(in meta, _submitIdx);
        _submitIdx++;
    }
    
    public void SubmitDrawBatch<T>(ReadOnlySpan<T> cmds, ReadOnlySpan<DrawCommandMeta> metas)
        where T : unmanaged, IDrawCommand
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(cmds.Length, metas.Length);

        int count = cmds.Length;
        if (count == 0) return;

        EnsureCapacity(count);

        int size   = Unsafe.SizeOf<T>();
        int offset = _submitIdx * _stride;
        var span    = _buffer.Span;

        // If stride = size we can bulk insert it
        if (_stride == size)
        {
            MemoryMarshal.AsBytes(cmds).CopyTo(span.Slice(offset, count * size));

            for (int i = 0; i < count; i++)
                _indices[_submitIdx + i] = new DrawCommandMetaIndex(in metas[i], _submitIdx + i);

            _submitIdx += count;
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var dst = span.Slice(offset + i * _stride, size);
            MemoryMarshal.Write(dst, in cmds[i]);
            _indices[_submitIdx + i] = new DrawCommandMetaIndex(in metas[i], _submitIdx + i);
        }
        _submitIdx += count;
    }

    public void Prepare()
    {
        if (_submitIdx <= 2) return;
        _indices.AsSpan(0, _submitIdx).Sort();
    }


    public void DrainCommandQueue(RenderTargetId targetId)
    {
        var bufferSpan = _buffer.Span;
        for (int i = _iteratorIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref _indices[_iteratorIdx++];
            if (it.Meta.Target < targetId) continue;
            if (it.Meta.Target > targetId) break;
            _registry.Dispatch(it.Meta.Id, bufferSpan, it.Idx, _stride);
        }
    }

    public void Reset()
    {
        _submitIdx = 0;
        _iteratorIdx = 0;
    }

    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;

        var sizeInBytes = idx * _stride;
        if (sizeInBytes >= _buffer.Length)
        {
            int newSize = Math.Max(sizeInBytes + DefaultBufferCapacity, _buffer.Length * 2);
            if (newSize > MaxBufferCapacity) throw new OutOfMemoryException("Command Buffer too big");
            var newArr = new byte[newSize];

            _buffer.Span.Slice(0, int.Min(_submitIdx * _stride, _buffer.Length)).CopyTo(newArr);
            _buffer = newArr;
        }

        if (idx >= _indices.Length)
        {
            int newSize = Math.Max(idx + DefaultIndicesCapacity, _indices.Length * 2);
            if (newSize > MaxIndicesCapacity) throw new OutOfMemoryException("DrawCommandMeta Buffer too big");
            var newArr = new DrawCommandMetaIndex[newSize];
            _indices.AsSpan(0, int.Min(_submitIdx, _indices.Length)).CopyTo(newArr);
            _indices = newArr;
        }
    }


    private class DispatchRegistry
    {
        private readonly ProcessDelegate[] _delegates = new ProcessDelegate[Enum.GetValues<DrawCommandId>().Length];
        
        internal void Register<T>(DrawCommandId cmdId) where T : unmanaged, IDrawCommand
        {
            _delegates[(int)cmdId] = ProcessHandler<T>;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(DrawCommandId cmdId, ReadOnlySpan<byte> buffer, int idx, int stride)
            => _delegates[(int)cmdId](cmdId, buffer, idx, stride);
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessHandler<T>(DrawCommandId commandId, ReadOnlySpan<byte> buffer, int idx, int stride) where T : unmanaged, IDrawCommand
        {
            var slot = buffer.Slice(idx * stride, Unsafe.SizeOf<T>());
            var payload = MemoryMarshal.Read<T>(slot);
            DrawProcessor.DrawDispatcher<T>.ExecuteDrawCall(in payload);
        }
    }


    private delegate void ProcessDelegate(DrawCommandId commandId, ReadOnlySpan<byte> buffer, int idx, int stride);
}