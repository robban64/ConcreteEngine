#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Renderer.Draw;

//internal delegate void DrawCommandDispatchDel(DrawCommand cmd, int idx);

public sealed class DrawCommandBuffer
{
    private DrawCommand[] _commandBuffer;
    private DrawObjectUniform[] _transformBuffer;
    private DrawCommandMeta[] _metaBuffer;
    private DrawCommandRef[] _indexBuffer;
    private DrawCommandTicket[] _drawTickets;
    private readonly DrawPassRange[] _passRanges;

    private int _submitIdx = 0;

    public int Count => _submitIdx;

    internal DrawCommandBuffer(DrawCommandProcessor cmdDraw, DrawBuffers drawBuffers)
    {
        _commandBuffer = new DrawCommand[DefaultCommandBuffCapacity];
        _transformBuffer = new DrawObjectUniform[DefaultCommandBuffCapacity];
        _metaBuffer = new DrawCommandMeta[DefaultCommandBuffCapacity];
        _indexBuffer = new DrawCommandRef[DefaultCommandBuffCapacity];
        _drawTickets = new DrawCommandTicket[DefaultCommandBuffCapacity];

        _passRanges = new DrawPassRange[PassSlots];

        _submitIdx = 0;
    }

    internal void Initialize()
    {
    }

    public void SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        in Matrix4x4 model,
        in Vector4 v0,
        in Vector4 v1,
        in Vector4 v2)
    {
        var idx = _submitIdx++;
        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);

        ref var drawUbo = ref _transformBuffer[idx];
        drawUbo.Model = model;
        drawUbo.NormalCol0 = v0;
        drawUbo.NormalCol1 = v1;
        drawUbo.NormalCol2 = v2;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ReadyDrawCommands()
    {
        if (_submitIdx <= 1)
        {
            _passRanges.AsSpan().Clear();
            return;
        }

        var indices = _indexBuffer.AsSpan(0, _submitIdx);
        indices.Sort();

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

        _passRanges.AsSpan().Clear();

        // Count pass ranges
        var total = 0;
        for (var p = 0; p < PassSlots; p++)
        {
            var c = counts[p];
            _passRanges[p] = new DrawPassRange(total, c);
            total += c;
        }

        // Create draw tickets
        EnsureTicketsCapacity(total);

        Span<int> heads = stackalloc int[PassSlots];
        for (var p = 0; p < PassSlots; p++)
            heads[p] = _passRanges[p].Start;

        // fill tickets in sorted order
        for (var i = 0; i < _submitIdx; i++)
        {
            ref readonly var mi = ref indices[i];
            var meta = metas[mi.Idx];
            var mask = (uint)meta.PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                _drawTickets[w] = new DrawCommandTicket(mi.Idx, (byte)p, meta.Resolver);
                mask &= mask - 1;
            }
        }
    }

    public ReadOnlySpan<DrawObjectUniform> DrainTransformQueue()
    {
        if (_transformBuffer.Length == 0) return ReadOnlySpan<DrawObjectUniform>.Empty;
        return _transformBuffer.AsSpan(0, _submitIdx);
    }


    internal void DispatchDrawPass(PassId passId, bool defaultDraw, DrawCommandProcessor cmd)
    {
        var pass = _passRanges[passId];
        var end = pass.Start + pass.Count;
        ReadOnlySpan<DrawCommand> commands = _commandBuffer.AsSpan();

        if (defaultDraw)
        {
            for (var i = pass.Start; i < end; i++)
            {
                var ticket = _drawTickets[i];
                cmd.DrawMesh(commands[ticket.SubmitIdx], ticket);
            }
            return;
        }
        
        for (var i = pass.Start; i < end; i++)
        {
            var ticket = _drawTickets[i];
            cmd.DrawSpecialResolveMesh(commands[ticket.SubmitIdx], ticket);
        }
    }

    public void Reset()
    {
        _submitIdx = 0;
    }

    public void EnsureBufferCapacity(int size)
    {
        if (_commandBuffer.Length >= size) return;
        var newCap = ArrayUtility.CapacityGrowthPow2(Math.Max(size, 64));

        if (newCap > MaxCommandBuffCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _commandBuffer, newCap);
        Array.Resize(ref _transformBuffer, newCap);
        Array.Resize(ref _metaBuffer, newCap);
        Array.Resize(ref _indexBuffer, newCap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;
        if (_commandBuffer.Length >= idx) return;
        var newCap = ArrayUtility.CapacityGrowthPow2(Math.Max(idx, 64));

        if (newCap > MaxCommandBuffCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _commandBuffer, newCap);
        Array.Resize(ref _transformBuffer, newCap);
        Array.Resize(ref _metaBuffer, newCap);
        Array.Resize(ref _indexBuffer, newCap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTicketsCapacity(int total)
    {
        if (_drawTickets.Length >= total) return;
        var newSize = ArrayUtility.CapacityGrowthLinear(_drawTickets.Length, total, step: PassSlots);
        _drawTickets = new DrawCommandTicket[newSize];
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Command Buffer exceeded max limit");
}