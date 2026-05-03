using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Buffer;

internal sealed class DrawCommandBufferRanges : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public NativeArray<int> DrawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
    public NativeArray<int> CountHeads = NativeArray.Allocate<int>(PassSlots * 2);
    public readonly Range32[] PassRanges = new Range32[PassSlots];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FillPassRanges()
    {
        var total = 0;
        for (var p = 0; p < PassSlots; p++)
        {
            var c = CountHeads[p];
            PassRanges[p] = new Range32(total, c);
            total += c;
        }

        for (var p = 0; p < PassSlots; p++)
            CountHeads[PassSlots + p] = PassRanges[p].Offset;

        return total;
    }


    public void EnsureTicketsCapacity(int total)
    {
        if (DrawTickets.Length >= total) return;
        var newSize = Arrays.CapacityGrowthSafe(DrawTickets.Length, total, largeThreshold: 16384);
        DrawTickets.Resize(newSize, false);
        Console.WriteLine("DrawTickets buffer resize");
    }

    public void Dispose()
    {
        DrawTickets.Dispose();
        CountHeads.Dispose();
    }
}

public sealed class DrawCommandBuffer : IDisposable
{
    private const int DefaultCommandBuffCapacity = 512;

    private const int DefaultBoneBufferCap = BoneCapacity * 64 * 10;

    private int _submitCmdIdx;
    private int _skeletonIdx;

    private NativeArray<DrawCommand> _commandBuffer;
    private NativeArray<DrawCommandMeta> _metaBuffer;
    private NativeArray<DrawCommandRef> _indexBuffer;

    private NativeArray<DrawObjectUniform> _transformBuffer;
    private NativeArray<Matrix4x4> _boneTransformBuffer;

    private readonly DrawCommandBufferRanges _bufferRanges = new();

    internal DrawCommandBuffer()
    {
        _commandBuffer = NativeArray.Allocate<DrawCommand>(DefaultCommandBuffCapacity);
        _metaBuffer = NativeArray.Allocate<DrawCommandMeta>(DefaultCommandBuffCapacity);
        _indexBuffer = NativeArray.Allocate<DrawCommandRef>(DefaultCommandBuffCapacity);

        _transformBuffer = NativeArray.Allocate<DrawObjectUniform>(DefaultCommandBuffCapacity);
        _boneTransformBuffer = NativeArray.Allocate<Matrix4x4>(DefaultBoneBufferCap);

        _submitCmdIdx = 0;
    }

    public int Count => _submitCmdIdx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Matrix4x4> WriteBones()
    {
        var index = _skeletonIdx++;
        return _boneTransformBuffer.Slice(index * BoneCapacity, BoneCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeZippedSpan<DrawCommand, DrawCommandMeta> GetDrawCommands(int start) =>
        new(ref _commandBuffer[start], ref _metaBuffer[start], _commandBuffer.Length - start);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Submit(DrawCommand cmd, DrawCommandMeta meta, in DrawObjectUniform matrices)
    {
        var idx = _submitCmdIdx++;
        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        _transformBuffer[idx] = matrices;
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform SubmitDraw()
    {
        var index = _submitCmdIdx++;
        _indexBuffer[index] = new DrawCommandRef(_metaBuffer[index], index);
        return ref _transformBuffer[index];
    }

    private bool Prepare()
    {
        if (_submitCmdIdx <= 1)
        {
            Array.Clear(_bufferRanges.PassRanges);
            return false;
        }

        var len = _submitCmdIdx;
        if ((uint)len > (uint)_metaBuffer.Length || (uint)len > (uint)_indexBuffer.Length)
            throw new InvalidOperationException();

        _bufferRanges.CountHeads.Clear();
        _indexBuffer.AsSpan(0, len).Sort();
        Array.Clear(_bufferRanges.PassRanges);

        return true;
    }

    internal unsafe void ReadyDrawCommands()
    {
        if (!Prepare()) return;

        var length = _submitCmdIdx;
        var heads = _bufferRanges.CountHeads.Ptr;

        // Count pass tickets
        for (var i = 0; i < _submitCmdIdx; i++)
        {
            var idx = _indexBuffer[i].Idx;
            var mask = (uint)_metaBuffer[idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                heads[p]++;
                mask &= mask - 1;
            }
        }

        // Count pass ranges
        var total = _bufferRanges.FillPassRanges();

        // Create draw tickets
        _bufferRanges.EnsureTicketsCapacity(total);

        heads += PassSlots;

        // fill tickets in sorted order
        for (var i = 0; i < length; i++)
        {
            var idx = _indexBuffer[i].Idx;
            var mask = (uint)_metaBuffer[idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                _bufferRanges.DrawTickets[w] = idx;
                mask &= mask - 1;
            }
        }
    }

    internal NativeView<DrawObjectUniform> DrainTransformBuffer()
    {
        var len = _submitCmdIdx;
        if (_transformBuffer.Length == 0) return NativeView<DrawObjectUniform>.MakeNull();
        if ((uint)len > (uint)_transformBuffer.Length) throw new IndexOutOfRangeException();

        return _transformBuffer.Slice(0, len);
    }

    internal NativeView<Matrix4x4> DrainBoneTransformBuffer()
    {
        var len = _skeletonIdx * BoneCapacity;
        if (_boneTransformBuffer.Length == 0) return NativeView<Matrix4x4>.MakeNull();
        if ((uint)len > (uint)_boneTransformBuffer.Length) throw new IndexOutOfRangeException();

        return _boneTransformBuffer.Slice(0, _skeletonIdx * BoneCapacity);
    }

    internal unsafe void DispatchDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _bufferRanges.PassRanges[passId];
        var tickets = _bufferRanges.DrawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawMesh(ref _commandBuffer[ticket], ticket);
        }
    }

    internal unsafe void DispatchResolveDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _bufferRanges.PassRanges[passId];
        var tickets = _bufferRanges.DrawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawSpecialResolveMesh(ref _commandBuffer[ticket], ticket);
        }
    }


    internal void Reset()
    {
        _submitCmdIdx = 0;
        _skeletonIdx = 0;
    }

    public void EnsureBufferCapacity(int size)
    {
        if (_commandBuffer.Length >= size) return;

        var newCap = Arrays.CapacityGrowthSafe(_commandBuffer.Length, size);

        if (newCap > MaxCommandBuffCapacity)
            ThrowMaxCapacityExceeded();

        _commandBuffer.Resize(newCap, true);
        _metaBuffer.Resize(newCap, true);
        _indexBuffer.Resize(newCap, true);
        _transformBuffer.Resize(newCap, false);

        Console.WriteLine("Command buffer resize");
    }

    public void EnsureBoneBuffer(int index)
    {
        var len = index * BoneCapacity;
        if (_boneTransformBuffer.Length >= len) return;
        var newSize = Arrays.CapacityGrowthSafe(_boneTransformBuffer.Length, len);
        _boneTransformBuffer.Resize(newSize, false);
        Console.WriteLine("BoneBuffer buffer resize");
    }


    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Command Buffer exceeded max limit");

    public void Dispose()
    {
        _transformBuffer.Dispose();
        _boneTransformBuffer.Dispose();
        _bufferRanges.Dispose();
    }
}