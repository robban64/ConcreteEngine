#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPipeline
{
    private const int DefaultCapacity = 64;
    private const int MaxCapacity = 10_000;

    private readonly DrawProcessor _drawProcessor;

    private DrawCommand[] _commands;
    private DrawTransformPayload[] _transforms;
    private DrawCommandMetaIndex[] _indices;

    private int _submitIdx = 0;
    private int _drainTransformIdx = 0;
    private int _drainCmdIdx = 0;
    
    public int Count => _submitIdx;

    internal RenderPipeline(DrawProcessor drawProcessor)
    {
        _drawProcessor = drawProcessor;
        _commands = new DrawCommand[DefaultCapacity];
        _transforms  = new DrawTransformPayload[DefaultCapacity];
        _indices = new DrawCommandMetaIndex[DefaultCapacity];
        _submitIdx = 0;
    }

    internal void Initialize() {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw(in DrawCommand cmd, in DrawCommandMeta meta, in DrawTransformPayload transform) 
    {
        EnsureCapacity(1);
        _commands[_submitIdx] = cmd;
        _transforms[_submitIdx] = transform;
        _indices[_submitIdx] = new DrawCommandMetaIndex(in meta, _submitIdx);
        _submitIdx++;
    }
    
    public void SubmitDrawBatch(ReadOnlySpan<DrawCommand> cmds, ReadOnlySpan<DrawCommandMeta> metas, ReadOnlySpan<DrawTransformPayload> transforms)
    {
        Debug.Assert(cmds.Length == metas.Length);
        Debug.Assert(cmds.Length == transforms.Length);

        int count = cmds.Length;
        if (count == 0) return;

        EnsureCapacity(count);
        cmds.CopyTo(_commands.AsSpan(_submitIdx));
        transforms.CopyTo(_transforms.AsSpan(_submitIdx));

        var indices = _indices.AsSpan(_submitIdx);
        for (int i = 0; i < count; i++)
        {
            indices[i] = new DrawCommandMetaIndex(in metas[i], _submitIdx + i);
        }

        _submitIdx += count;
    }

    public void Prepare()
    {
        if (_submitIdx <= 2) return;
        _indices.AsSpan(0, _submitIdx).Sort();
    }

    public void DrainTransformQueue()
    {
        var transforms = (ReadOnlySpan<DrawTransformPayload>)_transforms;
        var metaSpan = (ReadOnlySpan<DrawCommandMetaIndex>)_indices;

        for (int i = _drainTransformIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref metaSpan[_drainTransformIdx++];
            _drawProcessor.UploadTransform(in transforms[it.Idx]);
        }
    }


    public void DrainCommandQueue(RenderTargetId targetId)
    {
        var cmdSpan = (ReadOnlySpan<DrawCommand>)_commands;
        var metaSpan = (ReadOnlySpan<DrawCommandMetaIndex>)_indices;

        for (int i = _drainCmdIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref metaSpan[_drainCmdIdx++];
            if (it.Meta.Target < targetId) continue;
            if (it.Meta.Target > targetId) break;
            ref readonly var cmd = ref cmdSpan[it.Idx];
            _drawProcessor.DrawMesh(in cmd);
        }
    }

    public void Reset()
    {
        _submitIdx = 0;
        _drainTransformIdx = 0;
        _drainCmdIdx = 0;
    }

    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;

        if (idx >= _commands.Length)
        {
            int newSize = Math.Max(idx + DefaultCapacity, _commands.Length * 2);
            if (newSize > MaxCapacity) throw new OutOfMemoryException("Command Buffer too big");
            var newArr = new DrawCommand[newSize];
            _commands.AsSpan(0, int.Min(_submitIdx, _commands.Length)).CopyTo(newArr);
            _commands = newArr;
        }
        
        if (idx >= _transforms.Length)
        {
            int newSize = Math.Max(idx + DefaultCapacity, _transforms.Length * 2);
            if (newSize > MaxCapacity) throw new OutOfMemoryException("Transform Buffer too big");
            var newArr = new DrawTransformPayload[newSize];
            _transforms.AsSpan(0, int.Min(_submitIdx, _transforms.Length)).CopyTo(newArr);
            _transforms = newArr;
        }

        if (idx >= _indices.Length)
        {
            int newSize = Math.Max(idx + DefaultCapacity, _indices.Length * 2);
            if (newSize > MaxCapacity) throw new OutOfMemoryException("Index Buffer too big");
            var newArr = new DrawCommandMetaIndex[newSize];
            _indices.AsSpan(0, int.Min(_submitIdx, _indices.Length)).CopyTo(newArr);
            _indices = newArr;
        }
    }

}