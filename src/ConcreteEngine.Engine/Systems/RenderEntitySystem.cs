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

internal sealed class RenderEntitySystem : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public int VisibleCount { get; private set; }
    private int _drawCount;

    private readonly RenderFrustum _frustum;

    private NativeArray<ulong> _sortKeys;

    // draw data
    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;
    private NativeArray<TransformUniform> _transformBuffer;

    internal RenderEntitySystem(RenderFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;

        _sortKeys = NativeArray.Allocate<ulong>(RenderEcs.Core.Capacity);
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);

        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
        _passRanges = new Range32[RenderLimits.DrawPassSlots];
    }

    private NativeView<DrawEntityKey> SortKeys => _sortKeys.Slice(0, VisibleCount).Reinterpret<DrawEntityKey>();
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
        _drawIndices.Dispose();
        _sortKeys.Dispose();
        _transformBuffer.Dispose();
    }

/*
    private void CullEntities()
    {
        var frustum = _frustum;
        foreach (var query in RenderEcs.Core.CullQuery(EntityDrawStatus.Normal))
        {
            var passMask = query.Status == EntityDrawStatus.AlwaysVisible
                ? query.OriginalPasses
                : frustum.Intersects(query.OriginalPasses, in query.Bounds);

            if (passMask != 0)
                query.DrawPasses = passMask;
            else if (passMask != query.OriginalPasses)
                query.DrawPasses = 0;
        }
    }
    private unsafe int BuildVisibleIndices()
    {
        CameraManager.Instance.Camera.ExtractDepthKeyData(out var forward, out var scale, out var bias);

        var indices = (DrawEntityKey*)_sortIndices.Ptr;
        foreach (var query in RenderEcs.Core.VisibilityBoundsQuery(PassMask.Depth | PassMask.Main | PassMask.Effect))
        {
            var d = Vector4.Dot(forward, Unsafe.As<BoundingAxisBox, Vector4>(ref query.Item1));
            ushort distance = CalculateDepthKey(d, scale, bias);
            *indices++ = DrawEntityKey.Create(query.Entity, query.Passes, distance, query.Queue);
        }

        return (int)(indices - (DrawEntityKey*)_sortIndices.Ptr);
    }
*/
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort MakeDepthKeyU16(Vector3 forward, Vector3 worldPos, Vector2 nearFar, float viewZ)
    {
        const ushort maxValue = 65535;
        var d = Vector3.Dot(forward, worldPos) - viewZ;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return maxValue;
        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * 65535f + 0.5f);
    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private static ushort CalculateDepthKey(float dot, float scale, float bias)
   {
       const float maxValue = 65535f;
       float key = float.FusedMultiplyAdd(dot, scale, bias);
       return (ushort)float.MinNative(0f, float.MaxNative(key, maxValue));
   }
    */
}