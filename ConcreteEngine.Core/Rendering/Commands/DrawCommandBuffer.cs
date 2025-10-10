#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;
using static ConcreteEngine.Core.Rendering.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class DrawCommandBuffer
{
    private readonly DrawProcessor _drawProcessor;

    private DrawCommand[] _commandBuffer;
    private DrawTransformPayload[] _transformBuffer;
    private DrawCommandMeta[] _metaBuffer;
    private DrawCommandRef[] _indexBuffer;
    private DrawCommandTicket[] _drawTickets;
    private DrawPassRange[] _passRanges;

    private int _submitIdx = 0;
    private int _drainTransformIdx = 0;

    public int Count => _submitIdx;

    internal DrawCommandBuffer(DrawProcessor drawProcessor)
    {
        _commandBuffer = new DrawCommand[DefaultCommandBuffCapacity];
        _transformBuffer = new DrawTransformPayload[DefaultCommandBuffCapacity];
        _metaBuffer = new DrawCommandMeta[DefaultCommandBuffCapacity];
        _indexBuffer = new DrawCommandRef[DefaultCommandBuffCapacity];

        _drawTickets = new DrawCommandTicket[DefaultCommandBuffCapacity];

        _passRanges = new DrawPassRange[PassSlots];

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
        _commandBuffer[_submitIdx] = cmd;
        _transformBuffer[_submitIdx] = transform;
        _metaBuffer[_submitIdx] = meta;
        _indexBuffer[_submitIdx] = new DrawCommandRef(meta, _submitIdx);
        _submitIdx++;
    }

    public void SubmitDrawBatch(in DrawCommandData data)
    {
        Debug.Assert(data.Draw.Length == data.Meta.Length);
        Debug.Assert(data.Draw.Length == data.Transform.Length);

        var drawCommands = data.Draw;
        var drawTransforms = data.Transform;
        var drawMeta = data.Meta;
        
        var count = drawCommands.Length;
        if (count == 0) return;

        EnsureCapacity(count);
        drawCommands.CopyTo(_commandBuffer.AsSpan(_submitIdx));
        drawTransforms.CopyTo(_transformBuffer.AsSpan(_submitIdx));
        drawMeta.CopyTo(_metaBuffer.AsSpan(_submitIdx));

        var indices = _indexBuffer.AsSpan(_submitIdx);
        for (var i = 0; i < count; i++)
        {
            indices[i] = new DrawCommandRef(drawMeta[i], _submitIdx + i);
        }

        _submitIdx += count;
    }

    private HashSet<int> asd = new HashSet<int>(1000);
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ReadyDrawCommands()
    {
        if (_submitIdx <= 1) return;
        var indices = _indexBuffer.AsSpan(0, _submitIdx);
        indices.Sort();

        Array.Fill(_passRanges, new DrawPassRange(0, 0));

        // Count pass tickets
        Span<int> counts = stackalloc int[PassSlots];
        var metas = _metaBuffer.AsSpan(0, _submitIdx);
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
        for (var p = 0; p < PassSlots; p++)
        {
            var c = counts[p];
            _passRanges[p] = new DrawPassRange(total, c);
            total += c;
        }

        // Create draw tickets
        if (_drawTickets.Length < total)
        {
            _drawTickets = new DrawCommandTicket[ArrayUtility.CapacityGrowthPow2(total)];
        }

        Span<int> heads = stackalloc int[PassSlots];
        for (var p = 0; p < PassSlots; p++)
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
                asd.Add(w);
                _drawTickets[w] = new DrawCommandTicket(mi.Idx, (byte)p);
                mask &= mask - 1;
            }
        }
    }


    //TODO bulk upload
    public void DrainTransformQueue()
    {
        var transforms = (ReadOnlySpan<DrawTransformPayload>)_transformBuffer;
        var indices = (ReadOnlySpan<DrawCommandRef>)_indexBuffer;

        for (var i = _drainTransformIdx; i < _submitIdx; i++)
        {
            ref readonly var it = ref indices[_drainTransformIdx++];
            _drawProcessor.UploadTransform(in transforms[it.Idx], it.Idx);
        }
    }

    public void DispatchDrawPass(PassId passId)
    {
        var pass = _passRanges[passId];
        var end = pass.Start + pass.Count;
        ReadOnlySpan<DrawCommand> commands = _commandBuffer.AsSpan();
        for (var i = pass.Start; i < end; i++)
        {
            var idx = _drawTickets[i].SubmitIdx;
            _drawProcessor.DrawMesh(commands[idx], idx);
        }
    }
/*
    public void DrainCommandQueue(RenderTargetId targetId)
    {
        var cmdSpan = (ReadOnlySpan<DrawCommand>)_commandBuffer;
        var metaSpan = (ReadOnlySpan<DrawCommandRef>)_indexBuffer;

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
    }

    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;
        if (_commandBuffer.Length >= idx) return;
        var newCap = ArrayUtility.CapacityGrowthPow2(Math.Max(idx, 4));

        if (newCap > MaxCommandBuffCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _commandBuffer, newCap);
        Array.Resize(ref _transformBuffer, newCap);
        Array.Resize(ref _metaBuffer, newCap);
        Array.Resize(ref _indexBuffer, newCap);
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded()
        => throw new OutOfMemoryException("Command Buffer too big");
}