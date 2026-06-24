using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;
using static ConcreteEngine.Renderer.RenderLimits;

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
        var newSize = CapacityUtils.CapacityGrowthToFit(DrawTickets.Length, total);
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
    private const int DefaultTicketCapacity = 1024 * 4;

    private static bool _allocated = false;

    public int Count { get; private set; }

    private NativeArray<DrawCommand> _commands;
    private NativeArray<DrawCommandMeta> _metas;
    private NativeArray<DrawCommandRef> _indices;

    private NativeArray<DrawObjectUniform> _transforms;

    private NativeArray<int> _drawTickets;

    private readonly Range32[] _passRanges;


    internal DrawCommandBuffer()
    {
        if (_allocated) throw new InvalidOperationException("Already allocated");
        _commands = NativeArray.Allocate<DrawCommand>(DefaultCommandBuffCapacity);
        _metas = NativeArray.Allocate<DrawCommandMeta>(DefaultCommandBuffCapacity);
        _indices = NativeArray.Allocate<DrawCommandRef>(DefaultCommandBuffCapacity);
        _transforms = NativeArray.AlignedAllocate<DrawObjectUniform>(DefaultCommandBuffCapacity, alignment: 16);
        _drawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
        _passRanges = new Range32[PassSlots];
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeZippedSpan<DrawCommand, DrawCommandMeta> GetCommandMetaSpan() =>
        new(ref _commands[Count], ref _metas[Count], _commands.Length - Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SubmitIdentity(DrawCommand cmd, DrawCommandMeta meta)
    {
        var idx = Count++;
        _commands[idx] = cmd;
        _metas[idx] = meta;
        _indices[idx] = new DrawCommandRef(meta, idx);
        _transforms[idx].Model = Matrix4x4.Identity;
        _transforms[idx].Normal = Matrix3X4.Identity;
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform SubmitDraw()
    {
        var index = Count++;
        _indices[index] = new DrawCommandRef(_metas[index], index);
        return ref _transforms[index];
    }

    internal unsafe void ReadyDrawCommands()
    {
        var length = Count;

        if (length <= 1) return;

        if ((uint)length > (uint)_metas.Length)
            Throwers.InvalidOperation();

        Array.Clear(_passRanges);

        _indices.AsSpan(0, length).Sort();

        var heads = stackalloc int[PassSlots * 2];

        // Count pass tickets
        CountTickets(heads, length);

        // Count pass ranges
        var total = CountPasses(heads);

        // Create draw tickets
        EnsureTicketsCapacity(total);

        // fill tickets in sorted order
        FillTickets(heads + PassSlots, length);
    }

    private unsafe void CountTickets(int* heads, int length)
    {
        var indices = _indices;
        var metas = _metas;

        for (var i = 0; i < length; i++)
        {
            var idx = indices[i].Index;
            var mask = (uint)metas[idx].Passes;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                heads[p]++;
                mask &= mask - 1;
            }
        }
    }

    private unsafe int CountPasses(int* heads)
    {
        var passRanges = _passRanges;

        var total = 0;
        for (var p = 0; p < PassSlots; p++)
        {
            var c = heads[p];
            var range = passRanges[p] = new Range32(total, c);
            heads[PassSlots + p] += range.Offset;
            total += c;
        }


        return total;
    }

    private unsafe void FillTickets(int* heads, int length)
    {
        // fill tickets in sorted order
        var drawTickets = _drawTickets;
        var indices = _indices;
        var metas = _metas;

        for (var i = 0; i < length; i++)
        {
            var idx = indices[i].Index;
            var mask = (uint)metas[idx].Passes;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;
                drawTickets[w] = idx;
                mask &= mask - 1;
            }
        }
    }

    internal NativeView<DrawObjectUniform> DrainTransformBuffer()
    {
        var len = Count;
        if (_transforms.Length == 0) return NativeView<DrawObjectUniform>.MakeNull();
        if ((uint)len > (uint)_transforms.Length) Throwers.InvalidOperation();

        return _transforms.Slice(0, len);
    }

    internal unsafe void DispatchDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _passRanges[passId];
        var tickets = _drawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawMesh(_commands[ticket], ticket);
        }
    }

    internal unsafe void DispatchResolveDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _passRanges[passId];
        var tickets = _drawTickets + pass.Offset;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            ref readonly var meta = ref _metas[ticket];
            cmd.DrawSpecialResolveMesh(_commands[ticket], meta.Resolver, meta.ResolverSlot, ticket);
        }
    }


    internal void Reset() => Count = 0;

    private void EnsureTicketsCapacity(int total)
    {
        if (_drawTickets.Length >= total) return;
        var newSize = CapacityUtils.CapacityGrowthToFit(_drawTickets.Length, total);
        _drawTickets.Resize(newSize, false);
        Console.WriteLine("DrawTickets buffer resize");
    }

    public void EnsureCapacity(int size)
    {
        if (_commands.Length >= size) return;

        var newCap = CapacityUtils.CapacityGrowthToFit(_commands.Length, size);

        if (newCap > MaxCommandBuffCapacity)
            Throwers.BufferOverflow(nameof(DrawCommandBuffer), newCap, MaxCommandBuffCapacity);

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

        _drawTickets.Dispose();

        _allocated = false;
    }
}