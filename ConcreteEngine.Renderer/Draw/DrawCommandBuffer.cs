#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawCommandBuffer
{
    private const int DefaultTicketCapacity = 1024;

    private DrawCommand[] _commandBuffer;
    private DrawObjectUniform[] _transformBuffer;
    private DrawCommandMeta[] _metaBuffer;
    private DrawCommandRef[] _indexBuffer;
    private DrawCommandTicket[] _drawTickets;
    private readonly DrawPassRange[] _passRanges;

    private Matrix4x4[] _boneTransformBuffer;

    private DrawCommandProcessor _processor = null!;

    private int _submitCmdIdx = 0;
    private int _submitTransformIdx = 0;

    private int _skeletonIdx = 0;

    public int Count => _submitCmdIdx;

    internal DrawCommandBuffer()
    {
        _commandBuffer = new DrawCommand[DefaultCommandBuffCapacity];
        _transformBuffer = new DrawObjectUniform[DefaultCommandBuffCapacity];
        _metaBuffer = new DrawCommandMeta[DefaultCommandBuffCapacity];
        _indexBuffer = new DrawCommandRef[DefaultCommandBuffCapacity];
        _drawTickets = new DrawCommandTicket[DefaultTicketCapacity];

        _passRanges = new DrawPassRange[PassSlots];

        _boneTransformBuffer = new Matrix4x4[DefaultBoneBufferCap];

        _submitCmdIdx = 0;
    }

    internal void Initialize(DrawCommandProcessor cmd)
    {
        _processor = cmd;
    }

    public DrawCommandUploader GetDrawUploaderCtx() => new(this, _transformBuffer);
    public SkinningBufferUploader GetSkinningUploaderCtx() => new(this, _boneTransformBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int IncrementSkinningIndex() => _skeletonIdx++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int IncrementTransformIndex() => _submitTransformIdx++;

    internal int Submit(in DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = _submitCmdIdx++;
        if ((uint)idx >= _commandBuffer.Length)
        {
            throw new IndexOutOfRangeException();
        }

        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        return idx;
    }

    public int SubmitDrawIdentity(DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = Submit(in cmd, meta);
        _transformBuffer[idx].Model = Matrix4x4.Identity;
        _transformBuffer[idx].Normal = default;
        _submitTransformIdx++;
        return idx;
    }

    public int SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        in Matrix4x4 model,
        in Matrix3X4 normal)
    {
        var idx = Submit(in cmd, meta);
        ref var drawUbo = ref _transformBuffer[idx];
        drawUbo.Model = model;
        drawUbo.Normal = normal;
        _submitTransformIdx++;
        return idx;
    }


    public int SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        ref DrawObjectUniform drawUniform)
    {
        var idx = Submit(in cmd, meta);
        ref var data = ref Unsafe.AsRef(ref _transformBuffer[idx]);
        data = drawUniform;
        _submitTransformIdx++;
        return idx;
    }


    internal void ReadyDrawCommands()
    {
        if (_submitCmdIdx <= 1)
        {
            _passRanges.AsSpan().Clear();
            return;
        }

        if (_submitTransformIdx != _submitCmdIdx)
        {
            throw new InvalidOperationException(
                $"Submitted commands and transform don't match in length: cmd={_submitCmdIdx} - transform={_submitTransformIdx}");
        }

        var len = _submitCmdIdx;
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

    internal ReadOnlySpan<DrawObjectUniform> DrainTransformBuffer()
    {
        var len = _submitCmdIdx;
        if (_transformBuffer.Length == 0) return ReadOnlySpan<DrawObjectUniform>.Empty;
        if ((uint)len > _transformBuffer.Length) throw new IndexOutOfRangeException();

        return _transformBuffer.AsSpan(0, _submitCmdIdx);
    }

    internal ReadOnlySpan<Matrix4x4> DrainBoneTransformBuffer()
    {
        var len = _skeletonIdx * BoneCapacity;
        if (_boneTransformBuffer.Length == 0) return ReadOnlySpan<Matrix4x4>.Empty;
        if ((uint)len > _boneTransformBuffer.Length) throw new IndexOutOfRangeException();

        return _boneTransformBuffer.AsSpan(0, _skeletonIdx * BoneCapacity);
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

    internal void Reset()
    {
        _submitCmdIdx = 0;
        _submitTransformIdx = 0;
        _skeletonIdx = 0;
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

    public void EnsureBoneBuffer(int index)
    {
        var len = index * BoneCapacity;
        if (_boneTransformBuffer.Length >= len) return;
        var newSize = Arrays.CapacityGrowthSafe(_boneTransformBuffer.Length, len);
        Array.Resize(ref _boneTransformBuffer, newSize);
        Console.WriteLine("BoneBuffer buffer resize");
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