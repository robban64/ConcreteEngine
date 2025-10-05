#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class DrawCommandBuffer
{
    private const int RangesCount = 32;
    private const int DefaultCapacity = 64;
    private const int MaxCapacity = 10_000;

    private readonly DrawProcessor _drawProcessor;

    private DrawCommand[] _commands;
    private DrawTransformPayload[] _transforms;
    private DrawCommandMeta[] _metas;

    private DrawCommandMetaIndex[] _indices;

    private DrawTicket[] _tickets;
    private readonly PassRange[] _passRanges;

    private int _submitIdx = 0;
    private int _drainTransformIdx = 0;
    private int _drainCmdIdx = 0;

    public int Count => _submitIdx;

    internal DrawCommandBuffer(DrawProcessor drawProcessor)
    {
        _commands = new DrawCommand[DefaultCapacity];
        _transforms = new DrawTransformPayload[DefaultCapacity];
        _metas = new DrawCommandMeta[DefaultCapacity];
        _indices = new DrawCommandMetaIndex[DefaultCapacity];

        _tickets = new DrawTicket[DefaultCapacity];

        _passRanges = new PassRange[RangesCount];

        _drawProcessor = drawProcessor;

        _submitIdx = 0;
    }

    internal void Initialize()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw(DrawCommand cmd, DrawCommandMeta meta, in DrawTransformPayload transform)
    {
        EnsureCapacity(1);
        _commands[_submitIdx] = cmd;
        _transforms[_submitIdx] = transform;
        _metas[_submitIdx] = meta;
        _indices[_submitIdx] = new DrawCommandMetaIndex(meta, _submitIdx);
        _submitIdx++;
    }

    public void SubmitDrawBatch(ReadOnlySpan<DrawCommand> cmds, ReadOnlySpan<DrawCommandMeta> metas,
        ReadOnlySpan<DrawTransformPayload> transforms)
    {
        Debug.Assert(cmds.Length == metas.Length);
        Debug.Assert(cmds.Length == transforms.Length);

        var count = cmds.Length;
        if (count == 0) return;

        EnsureCapacity(count);
        cmds.CopyTo(_commands.AsSpan(_submitIdx));
        transforms.CopyTo(_transforms.AsSpan(_submitIdx));
        metas.CopyTo(_metas.AsSpan(_submitIdx));

        var indices = _indices.AsSpan(_submitIdx);
        for (var i = 0; i < count; i++)
        {
            indices[i] = new DrawCommandMetaIndex(metas[i], _submitIdx + i);
        }

        _submitIdx += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ReadyDrawCommands()
    {
        if (_submitIdx <= 1) return;
        var indices = _indices.AsSpan(0, _submitIdx);
        indices.Sort();

        Array.Fill(_passRanges, new PassRange(0, 0));

        // Count pass tickets
        Span<int> counts = stackalloc int[32];
        var metas = _metas.AsSpan(0, _submitIdx);
        for (var i = 0; i < _submitIdx; i++)
        {
            ref readonly var index = ref indices[i];
            var mask = (uint)metas[index.Idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                counts[p]++;
                mask &= mask - 1;
            }
        }

        // Count pass ranges
        var total = 0;
        for (var p = 0; p < 32; p++)
        {
            var c = counts[p];
            _passRanges[p] = new PassRange(total, c);
            total += c;
        }

        // Create draw tickets
        if (_tickets.Length < total)
            _tickets = new DrawTicket[ArrayUtility.CapacityGrowthPow2(total)];

        Span<int> heads = stackalloc int[32];
        for (var p = 0; p < 32; p++)
            heads[p] = _passRanges[p].Start;

        // fill tickets in sorted order
        for (var i = 0; i < _submitIdx; i++)
        {
            ref readonly var mi = ref indices[i];
            var mask = (uint)metas[mi.Idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                _tickets[w] = new DrawTicket(mi.Idx, (byte)p);
                mask &= mask - 1;
            }
        }
    }


    //TODO bulk upload
    public void DrainTransformQueue()
    {
        var transforms = (ReadOnlySpan<DrawTransformPayload>)_transforms;
        var indices = (ReadOnlySpan<DrawCommandMetaIndex>)_indices;

        for (var i = _drainTransformIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref indices[_drainTransformIdx++];
            _drawProcessor.UploadTransform(in transforms[it.Idx]);
        }
    }

    public void DispatchDrawPass(PassId passId)
    {
        var pass = _passRanges[passId];
        var end = pass.Start + pass.Count;
        ReadOnlySpan<DrawCommand> commands = _commands.AsSpan();
        for (var i = pass.Start; i < end; i++)
        {
            var idx = _tickets[i].SubmitIdx;
            _drawProcessor.DrawMesh(commands[idx]);
        }
    }
/*
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
*/

    public void Reset()
    {
        _submitIdx = 0;
        _drainTransformIdx = 0;
        _drainCmdIdx = 0;
    }

    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;
        if (_commands.Length >= idx) return;
        var newCap = ArrayUtility.CapacityGrowthPow2(Math.Max(idx, 4));

        if (newCap > MaxCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _commands, newCap);
        Array.Resize(ref _transforms, newCap);
        Array.Resize(ref _metas, newCap);
        Array.Resize(ref _indices, newCap);
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded()
        => throw new OutOfMemoryException("Command Buffer too big");
}