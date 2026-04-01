using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Draw;

/*
public readonly ref struct DrawCommandWriter(ref DrawCommand cmd, ref DrawCommandMeta meta, int length, int offset)
{
    private readonly ref DrawCommand _cmd = ref cmd;
    private readonly ref DrawCommandMeta _meta = ref meta;
    public readonly int Length = length;
    public readonly int Offset = offset;

    public ref DrawCommand GetCommand(int index) => ref Unsafe.Add(ref _cmd, Offset + index);
    public ref DrawCommandMeta GetMeta(int index) => ref Unsafe.Add(ref _meta, Offset + index);
}
*/
public sealed class DrawCommandBuffer : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;
    private const int DefaultCommandBuffCapacity = 512;

    private const int DefaultBoneBufferCap = BoneCapacity * 64 * 10;

    private readonly Range32[] _passRanges;

    private NativeArray<DrawCommand> _commandBuffer;
    private NativeArray<DrawCommandMeta> _metaBuffer;
    private NativeArray<DrawCommandRef> _indexBuffer;

    private NativeArray<int> _drawTickets;
    private NativeArray<int> _countHeads;

    private NativeArray<DrawObjectUniform> _transformBuffer;
    private NativeArray<Matrix4x4> _boneTransformBuffer;

    private int _submitCmdIdx;
    private int _skeletonIdx;

    private DrawCommandProcessor _processor = null!;

    internal DrawCommandBuffer()
    {
        _commandBuffer = NativeArray.Allocate<DrawCommand>(DefaultCommandBuffCapacity);
        _metaBuffer = NativeArray.Allocate<DrawCommandMeta>(DefaultCommandBuffCapacity);
        _indexBuffer = NativeArray.Allocate<DrawCommandRef>(DefaultCommandBuffCapacity);

        _drawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
        _countHeads = NativeArray.Allocate<int>(PassSlots * 2);

        _transformBuffer = NativeArray.Allocate<DrawObjectUniform>(DefaultCommandBuffCapacity);
        _boneTransformBuffer = NativeArray.Allocate<Matrix4x4>(DefaultBoneBufferCap);

        _passRanges = new Range32[PassSlots];

        _submitCmdIdx = 0;
    }

    public int Count => _submitCmdIdx;

    internal void Initialize(DrawCommandProcessor cmd) => _processor = cmd;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeViewPtr<Matrix4x4> GetBoneWriter()
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
            Array.Clear(_passRanges);
            return false;
        }

        var len = _submitCmdIdx;
        if ((uint)len > _metaBuffer.Length || (uint)len > _indexBuffer.Length)
            throw new InvalidOperationException();

        _countHeads.Clear();
        _indexBuffer.AsSpan(0, len).Sort();
        Array.Clear(_passRanges);

        return true;
    }

    internal unsafe void ReadyDrawCommands()
    {
        if (!Prepare()) return;

        var len = _submitCmdIdx;

        var heads = _countHeads.Ptr;

        // Count pass tickets
        for (var i = 0; i < len; i++)
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
        var total = 0;
        for (var p = 0; p < _passRanges.Length; p++)
        {
            var c = heads[p];
            _passRanges[p] = new Range32(total, c);
            total += c;
        }

        // Create draw tickets
        EnsureTicketsCapacity(total);

        heads += PassSlots;

        for (var p = 0; p < _passRanges.Length; p++)
            heads[p] = _passRanges[p].Offset;

        // fill tickets in sorted order
        for (var i = 0; i < len; i++)
        {
            var idx = _indexBuffer[i].Idx;
            var meta = _metaBuffer[idx];
            var mask = (uint)meta.PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                _drawTickets[w] = idx;
                mask &= mask - 1;
            }
        }
    }

    internal ReadOnlySpan<DrawObjectUniform> DrainTransformBuffer()
    {
        var len = _submitCmdIdx;
        if (_transformBuffer.Length == 0) return ReadOnlySpan<DrawObjectUniform>.Empty;
        if ((uint)len > (uint)_transformBuffer.Length) throw new IndexOutOfRangeException();

        return _transformBuffer.AsSpan(0, len);
    }

    internal ReadOnlySpan<Matrix4x4> DrainBoneTransformBuffer()
    {
        var len = _skeletonIdx * BoneCapacity;
        if (_boneTransformBuffer.Length == 0) return ReadOnlySpan<Matrix4x4>.Empty;
        if ((uint)len > (uint)_boneTransformBuffer.Length) throw new IndexOutOfRangeException();

        return _boneTransformBuffer.AsSpan(0, _skeletonIdx * BoneCapacity);
    }


    internal unsafe void DispatchDrawPass(PassId passId, bool defaultDraw)
    {
        var pass = _passRanges[passId];
        var tickets = _drawTickets.AsSpan(pass.Offset, pass.Length);

        var span = new UnsafeSpan<DrawCommand>(ref *_commandBuffer.Ptr, _commandBuffer.Length);

        if (!defaultDraw)
        {
            foreach (var ticket in tickets)
                _processor.DrawSpecialResolveMesh(ref span[ticket], ticket);

            return;
        }

        foreach (var ticket in tickets)
            _processor.DrawMesh(ref span[ticket], ticket);
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

    private void EnsureTicketsCapacity(int total)
    {
        if (_drawTickets.Length >= total) return;
        var newSize = Arrays.CapacityGrowthSafe(_drawTickets.Length, total, largeThreshold: 16384);
        _drawTickets.Resize(newSize, false);
        Console.WriteLine("DrawTickets buffer resize");
    }


    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Command Buffer exceeded max limit");

    public void Dispose()
    {
        _transformBuffer.Dispose();
        _boneTransformBuffer.Dispose();
        _drawTickets.Dispose();
        _countHeads.Dispose();
    }
}