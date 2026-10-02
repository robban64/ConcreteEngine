using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderDispatcher : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public int VisibleCount { get; private set; }

    private int _drawCount;

    // draw data
    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;
    private NativeArray<TransformUniform> _transformBuffer;

    private readonly RenderFrustum _frustum;
    private readonly EntityDataStore _renderData;

    internal RenderDispatcher(EntityDataStore renderData, RenderFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(renderData);
        ArgumentNullException.ThrowIfNull(frustum);

        _frustum = frustum;
        _renderData = renderData;

        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
        
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }


    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Slice(0, _drawCount).Reinterpret<DrawEntityIndex>();
    public NativeView<TransformUniform> Transforms => _transformBuffer.Slice(0, VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.Slice(range);
    }

    private AvgFrameTimer avg;

    public void Execute()
    {
        Ensure();
        
        avg.BeginSample();
        var visibleCount = CullEntities(RenderEcs.Core.Count);
        VisibleCount = visibleCount;
        if (avg.EndSample() > 144) avg.ResetAndPrint();

        if (visibleCount == 0) return;

        _renderData.RawSortKeys.AsSpan(0, visibleCount).Sort();
        
        BuildDrawIndices();
        FillTransformBuffer();
    }

    private int CullEntities(int length)
    {
        var frustum = _frustum;
        var sortKeys = _renderData.SortKeys.AsSpan().Slice(0, length);
        var policies = _renderData.Policies.AsReadOnlySpan().Slice(0, length);
        var worldBounds = _renderData.WorldBounds.AsReadOnlySpan().Slice(0, length);
        var visibilitySet = _renderData.VisibleSet;

        int count = 0;
        int blockCount = visibilitySet.BlockCount;
        for (int blockIndex = 0; blockIndex < blockCount; ++blockIndex)
        {
            var start = blockIndex * 64;
            var end = int.Min(start + 64, policies.Length);

            BitBlock block = default;
            for (int index = start; index < end; ++index)
            {
                var policy = policies[index];
                if (policy.Status == EntityDrawStatus.ForceHidden) continue;

                ref readonly var bounds = ref worldBounds[index];
                var culledPasses = frustum.Intersects(policy.Passes, in bounds, out var distance);
                var passes = policy.Status == EntityDrawStatus.AlwaysVisible ? policy.Passes : culledPasses;

                if (passes != 0)
                {
                    sortKeys[count++] = DrawEntityKey.Create(index, passes, distance, policy.Queue);
                    block.ToggleOn(index);
                }
            }

            visibilitySet.SetBlock(blockIndex, block.Block);
        }

        return count;
    }

    private unsafe void FillTransformBuffer()
    {
        var src = _renderData.Transforms.Ptr;
        var sortKeys = _renderData.SortKeys.Slice(0, VisibleCount);
        foreach (var it in sortKeys.Zip(Transforms))
        {
            it.Item2 = src[it.Item1.Entity];
        }
    }


    private unsafe void BuildDrawIndices()
    {
        var sortKeys = _renderData.SortKeys.AsSpan(0, VisibleCount);
        
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
        FillTickets(sortKeys,heads + RenderLimits.DrawPassSlots);
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
/*
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderDispatcher : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public int VisibleCount { get; private set; }
    
    private NativeArray<ulong> _sortKeys;

    private int _drawCount;
    private NativeArray<ulong> _drawIndices;

    private readonly Range32[] _passRanges;
    private NativeArray<TransformUniform> _transformBuffer;
    
    private readonly RenderFrustum _frustum;

    internal RenderDispatcher(RenderFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;

        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _sortKeys = NativeArray.Allocate<ulong>(RenderEcs.Core.Capacity);
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);

        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
    }

    private NativeView<DrawEntityKey> SortKeys => _sortKeys.Slice(0, VisibleCount).Reinterpret<DrawEntityKey>();
    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Slice(0, _drawCount).Reinterpret<DrawEntityIndex>();
    public NativeView<TransformUniform> Transforms => _transformBuffer.Slice(0, RenderEcs.Core.RenderSystem.VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.Slice(range);
    }
    private AvgFrameTimer avg;

    public void Execute()
    {
        Ensure();
        avg.BeginSample();
        var visibleCount = VisibleCount = CullEntities();
        if (avg.EndSample() > 144) avg.ResetAndPrint();

        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_sortKeys.Length);

        _sortKeys.AsSpan(0, VisibleCount).Sort();

        BuildDrawIndices();
        FillTransformBuffer();
    }

    private unsafe int CullEntities()
    {
        var frustum = _frustum;
        var indices = (DrawEntityKey*)_sortKeys.Ptr;

        var visibilitySet = RenderEcs.Core.VisibleSet;
        var policies = RenderEcs.Core.PolicySpan();
        var worldBounds = RenderEcs.Core.WorldBoundSpan();

        int blockCount = visibilitySet.BlockCount;
        for (int blockIndex = 0; blockIndex < blockCount; ++blockIndex)
        {
            var start = blockIndex * 64;
            var end = start + 64 <= policies.Length ? start + 64 : start + policies.Length & 63;
            
            if ((uint)start >= (uint)policies.Length) break;

            BitBlock block = default;
            for (int index = start; index < end; ++index)
            {
                var policy = policies[index];
                if (policy.Status < EntityDrawStatus.Normal) continue;

                ref readonly var bounds = ref worldBounds[index];
                var passes = frustum.Intersects(policy.Passes, in bounds, out var distance);
                passes = policy.Status == EntityDrawStatus.AlwaysVisible ? policy.Passes : passes;

                if (passes != 0)
                {
                    ushort depthKey = (ushort)float.Min(0f, float.Max(distance, 65535f));
                    *indices++ = DrawEntityKey.Create(index, passes, depthKey, policy.Queue);
                    block.ToggleOn(index);
                }
            }

            visibilitySet.SetBlock(blockIndex, block.Block);
        }

        return (int)(indices - (DrawEntityKey*)_sortKeys.Ptr);
    }

    private unsafe void FillTransformBuffer()
    {
        var src = RenderEcs.Core.TransformView().Ptr;
        foreach (var it in SortKeys.Zip(Transforms))
        {
            it.Item2 = src[it.Item1.Entity];
        }
    }


    private unsafe void BuildDrawIndices()
    {
        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.DrawPassSlots * 2];

        // Count pass tickets
        CountTickets(heads);

        // Count pass ranges
        var total = _drawCount = CountPasses(heads);

        if (_drawIndices.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawIndices.Length, total);
            _drawIndices.ReAlloc(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(heads + RenderLimits.DrawPassSlots);
    }


    private unsafe void CountTickets(int* heads)
    {
        var span = SortKeys.AsReadOnlySpan();
        foreach (var it in span)
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

    private unsafe void FillTickets(int* heads)
    {
        var drawTickets = DrawIndices.Ptr;
        var span = SortKeys.AsReadOnlySpan();
        for (int i = 0; i < span.Length; ++i)
        {
            var key = span[i];
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
        if (RenderEcs.Core.Capacity == _transformBuffer.Length) return;

        _sortKeys.ReAlloc(RenderEcs.Core.Capacity, true);
        _transformBuffer.ReAlloc(RenderEcs.Core.Capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _sortKeys.Dispose();
        _drawIndices.Dispose();
        _transformBuffer.Dispose();
    }
}*/