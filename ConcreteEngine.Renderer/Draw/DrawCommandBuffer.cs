#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawCommandBuffer
{
    private DrawCommand[] _commandBuffer;
    private DrawObjectUniform[] _transformBuffer;
    private DrawCommandMeta[] _metaBuffer;
    private DrawCommandRef[] _indexBuffer;
    private DrawCommandTicket[] _drawTickets;
    private readonly DrawPassRange[] _passRanges;

    private DrawCommandProcessor _processor = null!;
    private int _submitIdx = 0;

    public int Count => _submitIdx;

    internal DrawCommandBuffer()
    {
        _commandBuffer = new DrawCommand[DefaultCommandBuffCapacity];
        _transformBuffer = new DrawObjectUniform[DefaultCommandBuffCapacity];
        _metaBuffer = new DrawCommandMeta[DefaultCommandBuffCapacity];
        _indexBuffer = new DrawCommandRef[DefaultCommandBuffCapacity];
        _drawTickets = new DrawCommandTicket[DefaultCommandBuffCapacity];

        _passRanges = new DrawPassRange[PassSlots];

        _submitIdx = 0;
    }

    internal void Initialize(DrawCommandProcessor cmd)
    {
        _processor = cmd;
    }

    public void SubmitAnimationData(ReadOnlySpan<Matrix4x4> boneData, ReadOnlySpan<RangeU16> ranges)
    {
        _processor.SubmitAnimationData(boneData, ranges);
    }
    
    public void SubmitSingleAnimation(AnimationUniformWriter writer)
    {
        _processor.SubmitSingleAnimation(writer);
    }

    public void SubmitNonTransformDraw(DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = _submitIdx++;
        if ((uint)idx >= (uint)_commandBuffer.Length || (uint)idx >= (uint)_metaBuffer.Length ||
            (uint)idx >= (uint)_indexBuffer.Length || (uint)idx >= (uint)_transformBuffer.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        _transformBuffer[idx] = default;
    }

    public void SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        in Matrix4x4 model,
        in Matrix3X4 normal)
    {
        var idx = _submitIdx++;
        if ((uint)idx >= (uint)_commandBuffer.Length || (uint)idx >= (uint)_metaBuffer.Length ||
            (uint)idx >= (uint)_indexBuffer.Length || (uint)idx >= (uint)_transformBuffer.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);

        ref var drawUbo = ref _transformBuffer[idx];
        drawUbo.Model = model;
        drawUbo.Normal = normal;
    }


    public void SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        ref DrawObjectUniform drawUniform)
    {
        var idx = _submitIdx++;
        if ((uint)idx >= (uint)_commandBuffer.Length || (uint)idx >= (uint)_metaBuffer.Length)
            throw new IndexOutOfRangeException();

        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        ref var data = ref Unsafe.AsRef(ref _transformBuffer[idx]);
        data = drawUniform;
    }


    public void ReadyDrawCommands()
    {
        if (_submitIdx <= 1)
        {
            _passRanges.AsSpan().Clear();
            return;
        }

        var len = _submitIdx;
        var metas = _metaBuffer;
        var indices = _indexBuffer;

        if ((uint)len >= (uint)metas.Length || (uint)len >= (uint)indices.Length)
        {
            throw new IndexOutOfRangeException();
        }

        indices.AsSpan(0, len).Sort();

        // Count pass tickets
        Span<int> counts = stackalloc int[PassSlots];
        for (var i = 0; i < len; i++)
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
        for (var i = 0; i < len; i++)
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


    internal void DispatchDrawPass(PassId passId, bool defaultDraw)
    {
        var processor = _processor!;
        var pass = _passRanges[passId];

        var tickets = _drawTickets.AsSpan(pass.Start, pass.Count);
        var commands = _commandBuffer.AsSpan();

        if (defaultDraw)
        {
            foreach (var ticket in tickets)
                processor.DrawMesh(commands[ticket.SubmitIdx], ticket);

            return;
        }

        foreach (var ticket in tickets)
            processor.DrawSpecialResolveMesh(commands[ticket.SubmitIdx], ticket);
    }

    public void Reset()
    {
        _submitIdx = 0;
    }

    public void EnsureBufferCapacity(int size)
    {
        if (_commandBuffer.Length >= size) return;
        
        var newCap = Arrays.CapacityGrowthSafe(_commandBuffer.Length, size);

        if (newCap > MaxCommandBuffCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _commandBuffer, newCap);
        Array.Resize(ref _transformBuffer, newCap);
        Array.Resize(ref _metaBuffer, newCap);
        Array.Resize(ref _indexBuffer, newCap);
        
        Console.WriteLine("Command buffer resize");
    }


    private void EnsureTicketsCapacity(int total)
    {
        if (_drawTickets.Length >= total) return;
        var newSize = Arrays.CapacityGrowthLinear(_drawTickets.Length, total, step: PassSlots);
        _drawTickets = new DrawCommandTicket[newSize];
        
        Console.WriteLine("DrawTickets buffer resize");

    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Command Buffer exceeded max limit");
}