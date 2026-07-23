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

public sealed class DrawCommandBuffer : IDisposable
{
    private const int DefaultCommandBuffCapacity = 512;
    private const int DefaultTicketCapacity = 1024 * 4;

    private static bool _allocated = false;

    public int Count { get; private set; }

    private NativeSoA<DrawCommand, DrawCommandIndex> _commands;
    private NativeArray<DrawObjectUniform> _transforms;
    private NativeArray<int> _drawTickets;

    private readonly Range32[] _passRanges;


    internal DrawCommandBuffer()
    {
        if (_allocated) throw new InvalidOperationException("Already allocated");
        _allocated = true;
        Count = 0;

        _commands = new NativeSoA<DrawCommand, DrawCommandIndex>(DefaultCommandBuffCapacity);
        _transforms = NativeArray.AlignedAllocate<DrawObjectUniform>(DefaultCommandBuffCapacity, alignment: 16);
        _drawTickets = NativeArray.Allocate<int>(DefaultTicketCapacity);
        _passRanges = new Range32[PassSlots];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform TransformRef(int i) => ref _transforms[Count + i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawCommand CommandRef(int i) => ref _commands.At1(Count + i);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawCommandIndex IndexRef(int i) => ref _commands.At2(Count + i);

    public void IncrementDrawCount(int count) => Count += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SubmitIdentity(DrawCommand cmd, PassMask pass, DrawCommandQueue queue, ushort depthKey)
    {
        var idx = Count++;
        _commands.View1[idx] = cmd;
        _commands.View2[idx] = new DrawCommandIndex(idx, pass, queue, depthKey);
        _transforms[idx].Model = Matrix4x4.Identity;
        _transforms[idx].Normal = Matrix3X4.Identity;
        return idx;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawObjectUniform SubmitDraw() => ref _transforms[Count++];

    internal unsafe void ReadyDrawCommands()
    {
        var length = Count;

        if (length <= 1) return;

        if ((uint)length > (uint)_commands.Length)
            Throwers.InvalidOperation();

        Array.Clear(_passRanges);

        _commands.View2.AsSpan(0, length).Sort();

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
        var indices = _commands.View2;

        for (var i = 0; i < length; i++)
        {
            var mask = (uint)indices[i].Pass;
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
        var indices = _commands.View2;

        for (var i = 0; i < length; i++)
        {
            var idx = indices[i].Index;
            var mask = (uint)indices[i].Pass;
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
        var commands = _commands.View1;
        var passRange = _passRanges[passId];
        var ticket = _drawTickets + passRange.Offset;
        for (var i = 0; i < passRange.Length; ++i, ++ticket)
        {
            var submitIndex = *ticket;
            cmd.DrawMesh(commands[submitIndex], submitIndex);
        }
    }

    internal unsafe void DispatchResolveDrawPass(DrawCommandProcessor cmd, PassId passId)
    {
        var pass = _passRanges[passId];
        var tickets = _drawTickets + pass.Offset;
        var commands = _commands.View1;
        for (var i = 0; i < pass.Length; i++)
        {
            var ticket = tickets[i];
            cmd.DrawSpecialResolveMesh(commands[ticket], commands[ticket].Resolver, commands[ticket].ResolverSlot, ticket);
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
        _transforms.Resize(newCap, false);

        Console.WriteLine("Command buffer resize");
    }

    public void Dispose()
    {
        _commands.Dispose();
        _transforms.Dispose();

        _drawTickets.Dispose();

        _allocated = false;
    }
}