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

    public int Count { get; private set; }

    private NativeArray<DrawCommand> _commands;
    private NativeArray<DrawCommandMeta> _metas;
    private NativeArray<DrawCommandRef> _indices;

    private NativeArray<DrawObjectUniform> _transforms;

    private readonly DrawCommandBufferRanges _rangeBuffer = new();

    internal DrawCommandBuffer()
    {
        _commands = NativeArray.Allocate<DrawCommand>(DefaultCommandBuffCapacity);
        _metas = NativeArray.Allocate<DrawCommandMeta>(DefaultCommandBuffCapacity);
        _indices = NativeArray.Allocate<DrawCommandRef>(DefaultCommandBuffCapacity);
        _transforms = NativeArray.Allocate<DrawObjectUniform>(DefaultCommandBuffCapacity);

        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeZippedSpan<DrawCommand, DrawCommandMeta> GetDrawCommands(int start) =>
        new(ref _commands[start], ref _metas[start], _commands.Length - start);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Submit(DrawCommand cmd, DrawCommandMeta meta, in DrawObjectUniform matrices)
    {
        var idx = Count++;
        _commands[idx] = cmd;
        _metas[idx] = meta;
        _indices[idx] = new DrawCommandRef(meta, idx);
        _transforms[idx] = matrices;
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform SubmitDraw()
    {
        var index = Count++;
        _indices[index] = new DrawCommandRef(_metas[index], index);
        return ref _transforms[index];
    }

    private bool Prepare()
    {
        if (Count <= 1)
        {
            Array.Clear(_rangeBuffer.PassRanges);
            return false;
        }

        var len = Count;
        if ((uint)len > (uint)_metas.Length || (uint)len > (uint)_indices.Length)
            throw new InvalidOperationException();

        _rangeBuffer.CountHeads.Clear();
        _indices.AsSpan(0, len).Sort();
        Array.Clear(_rangeBuffer.PassRanges);

        return true;
    }

    internal unsafe void ReadyDrawCommands()
    {
        if (!Prepare()) return;

        var length = Count;
        var heads = _rangeBuffer.CountHeads.Ptr;

        // Count pass tickets
        for (var i = 0; i < Count; i++)
        {
            var idx = _indices[i].Index;
            var mask = (uint)_metas[idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                heads[p]++;
                mask &= mask - 1;
            }
        }

        // Count pass ranges
        var total = _rangeBuffer.FillPassRanges();

        // Create draw tickets
        _rangeBuffer.EnsureTicketsCapacity(total);

        heads += PassSlots;

        // fill tickets in sorted order
        for (var i = 0; i < length; i++)
        {
            var idx = _indices[i].Index;
            var mask = (uint)_metas[idx].PassMask;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                _rangeBuffer.DrawTickets[w] = idx;
                mask &= mask - 1;
            }
        }
    }

    internal NativeView<DrawObjectUniform> DrainTransformBuffer()
    {
        var len = Count;
        if (_transforms.Length == 0) return NativeView<DrawObjectUniform>.MakeNull();
        if ((uint)len > (uint)_transforms.Length) throw new IndexOutOfRangeException();

        return _transforms.Slice(0, len);
    }

    internal unsafe void DispatchDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _rangeBuffer.PassRanges[passId];
        var tickets = _rangeBuffer.DrawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawMesh(_commands[ticket], ticket);
        }
    }

    internal unsafe void DispatchResolveDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _rangeBuffer.PassRanges[passId];
        var tickets = _rangeBuffer.DrawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawSpecialResolveMesh(_commands[ticket], ticket);
        }
    }


    internal void Reset() => Count = 0;

    public void EnsureCapacity(int size)
    {
        if (_commands.Length >= size) return;

        var newCap = Arrays.CapacityGrowthSafe(_commands.Length, size);

        if (newCap > MaxCommandBuffCapacity)
            throw new InsufficientMemoryException("Command Buffer exceeded max limit");

        _commands.Resize(newCap, true);
        _metas.Resize(newCap, true);
        _indices.Resize(newCap, true);
        _transforms.Resize(newCap, false);

        Console.WriteLine("Command buffer resize");
    }

    public void Dispose()
    {
        _commands.Dispose();
        _metas.Dispose();
        _indices.Dispose();

        _transforms.Dispose();
        _rangeBuffer.Dispose();
    }
}