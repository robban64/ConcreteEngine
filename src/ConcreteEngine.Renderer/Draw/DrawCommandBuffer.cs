using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.Data.RenderLimits;

namespace ConcreteEngine.Renderer.Draw;

public sealed class DrawCommandBuffer : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;
    private const int DefaultCommandBuffCapacity = 512;

    private const int DefaultBoneBufferCap = BoneCapacity * 64 * 10;

    private readonly Range32[] _passRanges;

    private DrawCommand[] _commandBuffer;
    private DrawCommandMeta[] _metaBuffer;
    private DrawCommandRef[] _indexBuffer;

    private NativeArray<int> _drawTickets;
    private NativeArray<int> _countHeads;

    private NativeArray<DrawObjectUniform> _transformBuffer;
    private NativeArray<Matrix4x4> _boneTransformBuffer;

    private int _submitCmdIdx;
    private int _skeletonIdx;

    private DrawCommandProcessor _processor = null!;

    internal DrawCommandBuffer()
    {
        _commandBuffer = new DrawCommand[DefaultCommandBuffCapacity];
        _metaBuffer = new DrawCommandMeta[DefaultCommandBuffCapacity];
        _indexBuffer = new DrawCommandRef[DefaultCommandBuffCapacity];

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
    public UnsafeSpanSlice<Matrix4x4> GetBoneWriter()
    {
        var index = _skeletonIdx++;
        return new UnsafeSpanSlice<Matrix4x4>(ref _boneTransformBuffer[0], index * BoneCapacity, BoneCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Submit(in DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = _submitCmdIdx++;
        _commandBuffer[idx] = cmd;
        _metaBuffer[idx] = meta;
        _indexBuffer[idx] = new DrawCommandRef(meta, idx);
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform SubmitDraw(in DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = Submit(in cmd, meta);
        return ref _transformBuffer[idx];
    }

    public void SubmitDraw(
        DrawCommand cmd,
        DrawCommandMeta meta,
        in Matrix4x4 model,
        in Matrix3X4 normal)
    {
        var idx = Submit(in cmd, meta);
        ref var drawUbo = ref _transformBuffer[idx];
        drawUbo.Model = model;
        drawUbo.Normal = normal;
    }

    public int SubmitDrawIdentity(DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = Submit(in cmd, meta);
        ref var drawUbo = ref _transformBuffer[idx];
        drawUbo.Model = Matrix4x4.Identity;
        drawUbo.Normal = default;
        return idx;
    }


    internal unsafe void ReadyDrawCommands()
    {
        if (_submitCmdIdx <= 1)
        {
            Array.Clear(_passRanges);
            return;
        }

        var len = _submitCmdIdx;
        if ((uint)len > _metaBuffer.Length || (uint)len > _indexBuffer.Length)
            throw new InvalidOperationException();

        _countHeads.Clear();
        _indexBuffer.AsSpan(0, len).Sort();

        // Count pass tickets
        for (var i = 0; i < len; i++)
        {
            var idx = _indexBuffer[i].Idx;
            var mask = (uint)_metaBuffer[idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                _countHeads[p]++;
                mask &= mask - 1;
            }
        }

        Array.Clear(_passRanges);

        // Count pass ranges
        var total = 0;
        for (var p = 0; p < _passRanges.Length; p++)
        {
            var c = _countHeads[p];
            _passRanges[p] = new Range32(total, c);
            total += c;
        }

        // Create draw tickets
        EnsureTicketsCapacity(total);

        var heads = _countHeads + PassSlots;

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


    internal void DispatchDrawPass(PassId passId, bool defaultDraw)
    {
        var pass = _passRanges[passId];

        var tickets = _drawTickets.AsSpan(pass.Offset, pass.Length);

        if (!defaultDraw)
        {
            foreach (var ticket in tickets)
                _processor.DrawSpecialResolveMesh(ref _commandBuffer[ticket], ticket);

            return;
        }

        var span = new UnsafeSpan<DrawCommand>(_commandBuffer);
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

        Array.Resize(ref _commandBuffer, newCap);
        Array.Resize(ref _metaBuffer, newCap);
        Array.Resize(ref _indexBuffer, newCap);

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