using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render.Systems;

public sealed class RenderPassSystem : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    private int _drawCount;

    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;

    private readonly EntityDataStore _renderData;
    private readonly RenderCullSystem _cullSystem;

    internal RenderPassSystem(EntityDataStore renderData, RenderCullSystem cullSystem)
    {
        ArgumentNullException.ThrowIfNull(renderData);
        ArgumentNullException.ThrowIfNull(cullSystem);

        _renderData = renderData;
        _cullSystem = cullSystem;

        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
        
    }

    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Slice(0, _drawCount).Reinterpret<DrawEntityIndex>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.Slice(range);
    }

    public void Execute()
    {
        var visibleCount = _cullSystem.VisibleCount;
        if (visibleCount == 0) return;
        
        var sortKeys = _renderData.SortKeys.AsSpan(0, visibleCount);
        
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
        var drawTickets = DrawIndices.Ptr;
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


    public void Dispose() => _drawIndices.Dispose();
}