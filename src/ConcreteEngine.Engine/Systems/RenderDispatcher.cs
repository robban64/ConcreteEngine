using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderDispatcher : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    private int _drawCount;

    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;
    private NativeArray<TransformUniform> _transformBuffer;

    private readonly EntityDataStore _renderData;

    internal RenderDispatcher(EntityDataStore renderData)
    {
        ArgumentNullException.ThrowIfNull(renderData);

        _renderData = renderData;

        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
        
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Slice(0, _drawCount).Reinterpret<DrawEntityIndex>();
    public NativeView<TransformUniform> Transforms => _transformBuffer.Slice(0, RenderEcs.Core.CullSystem.VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.Slice(range);
    }


    public void Execute()
    {
        Ensure();
        
        var visibleCount = RenderEcs.Core.CullSystem.VisibleCount;
        if (visibleCount == 0) return;
        
        var sortKeys = _renderData.SortKeys.Slice(0, visibleCount);
        
        BuildDrawIndices(sortKeys);
        FillTransformBuffer(sortKeys);
    }
    
    private unsafe void FillTransformBuffer(NativeView<DrawEntityKey> sortKeys)
    {
        var src = _renderData.Transforms.Ptr;
        foreach (var it in sortKeys.Zip(Transforms))
        {
            it.Item2 = src[it.Item1.Entity];
        }
    }


    private unsafe void BuildDrawIndices(NativeView<DrawEntityKey> sortKeys)
    {
        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.DrawPassSlots * 2];

        // Count pass tickets
        CountTickets(sortKeys.AsSpan(), heads);

        // Count pass ranges
        var total = _drawCount = CountPasses(heads);

        if (_drawIndices.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawIndices.Length, total);
            _drawIndices.ReAlloc(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(sortKeys.AsSpan(), heads + RenderLimits.DrawPassSlots);
    }


    private unsafe void CountTickets(Span<DrawEntityKey> sortKeys, int* heads)
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

    private void Ensure()
    {
        var capacity = RenderEcs.Core.Capacity;
        if (capacity == _transformBuffer.Length) return;

        _transformBuffer.ReAlloc(capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _transformBuffer.Dispose();
        _drawIndices.Dispose();
    }

}