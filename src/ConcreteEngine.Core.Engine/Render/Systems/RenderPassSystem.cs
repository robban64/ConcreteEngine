using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Render.Systems;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawEntityIndex(int entity, int submitIndex)
{
    public readonly int Entity = entity;
    public readonly int SubmitIndex = submitIndex;
}

public sealed class RenderPassSystem : RenderWorldSystem
{
    private const int DefaultTicketCapacity = 1024 * 4;

    private int _drawCount;

    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;

    public RenderPassSystem()
    {
        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
    }

    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Reinterpret<DrawEntityIndex>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.AsReadOnlySpan(range.Offset, range.Length);
    }

    public void Execute(long frameId)
    {
        FrameVersion = frameId;
        
        if(RenderWorld.System<RenderCullSystem>().FrameVersion != frameId) Throwers.InvalidOperation();

        var visibleCount = RenderWorld.System<RenderCullSystem>().VisibleCount;
        if (visibleCount == 0) return;
        
        var sortKeys = RenderWorld.Dense<DrawEntityKey>().AsSpan().Slice(0, visibleCount);
        
        BuildDrawIndices(sortKeys);
    }

    private unsafe void BuildDrawIndices(Span<DrawEntityKey> sortKeys)
    {
        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.DrawPassSlots * 2];

        // Count pass tickets
        CountTickets(sortKeys, heads);

        // Count pass ranges
        var total = _drawCount = CountPasses(heads);

        if (_drawIndices.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawIndices.Length, total);
            _drawIndices.ReAlloc(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(sortKeys, heads + RenderLimits.DrawPassSlots);
    }


    private static unsafe void CountTickets(Span<DrawEntityKey> sortKeys, int* heads)
    {
        foreach (var it in sortKeys)
        {
            var mask = (uint)(byte)it.SortKey;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                ++heads[p];
                mask &= mask - 1;
            }
        }
    }

    private unsafe int CountPasses(int* heads)
    {
        var total = 0;
        for (var p = 0; p < RenderLimits.DrawPassSlots; ++p)
        {
            var c = heads[p];
            heads[RenderLimits.DrawPassSlots + p] += total;
            _passRanges[p] = new Range32(total, c);
            total += c;
        }

        return total;
    }

    private unsafe void FillTickets(Span<DrawEntityKey> sortKeys, int* heads)
    {
        var drawTickets = DrawIndices;
        for (int i = 0; i < sortKeys.Length; ++i)
        {
            var key = sortKeys[i];
            var index = new DrawEntityIndex(key.Entity, i);
            var mask = (uint)(byte)key.SortKey;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;

                drawTickets[w] = index;
                mask &= mask - 1;
            }
        }
    }


    public override void Dispose() => _drawIndices.Dispose();
}