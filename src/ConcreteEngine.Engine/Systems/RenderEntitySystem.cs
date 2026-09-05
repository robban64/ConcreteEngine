using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
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

    private readonly CameraFrustum _frustum;

    // draw data
    private readonly Range32[] _passRanges;
    private NativeArray<ulong> _drawIndices;

    // culled and sorted index
    private NativeArray<ulong> _sortIndices;

    // culled and sorted index
    private NativeArray<TransformUniform> _transformBuffer;

    internal RenderEntitySystem(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;

        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawIndices = NativeArray.Allocate<ulong>(DefaultTicketCapacity);
        _sortIndices = NativeArray.Allocate<ulong>(RenderEcs.Core.Capacity);
        _transformBuffer = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    private Span<DrawEntityKey> SortIndicesSpan => _sortIndices.Reinterpret<DrawEntityKey>().AsSpan(0, VisibleCount);
    private NativeView<DrawEntityIndex> DrawIndices => _drawIndices.Slice(0, _drawCount).Reinterpret<DrawEntityIndex>();
    public NativeView<TransformUniform> Transforms => _transformBuffer.Slice(0, VisibleCount);

    private ZipSpanEnumerator<DrawEntityKey, TransformUniform> TransformEnumerator() =>
        new(SortIndicesSpan, _transformBuffer.AsSpan(0, VisibleCount));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityIndex> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return DrawIndices.Slice(range);
    }

    public void Execute()
    {
        Ensure();
        CullEntities();
        var visibleCount = VisibleCount = BuildVisibleIndices();
        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_sortIndices.Length);

        _sortIndices.AsSpan(0, VisibleCount).Sort();

        BuildDrawIndices();
        FillTransformBuffer();
    }

    private void CullEntities()
    {
        var frustum = _frustum;
        foreach (var query in RenderEcs.Core.CullQuery(EntityDrawStatus.Normal))
        {
            var passMask = query.Status == EntityDrawStatus.AlwaysVisible
                ? query.OriginalPasses
                : frustum.Intersects(query.OriginalPasses, in query.Bounds);

            if (passMask != 0)
            {
                query.DrawPasses = passMask;
            }
            else if (query.OriginalPasses != passMask)
            {
                query.DrawPasses = 0;
            }
        }
    }


    private unsafe int BuildVisibleIndices()
    {
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);

        var indices = (DrawEntityKey*)_sortIndices.Ptr;
        foreach (var query in RenderEcs.Core.VisibilityBoundsQuery(PassMask.Depth | PassMask.Main | PassMask.Effect))
        {
            ref readonly var center = ref query.Item1.Center;
            var distance = MakeDepthKeyU16(forward, in center, nearFar, viewZ);
            var entityIndex = DrawEntityKey.Create(query.Entity, query.Passes, distance, query.Queue);
            *indices++ = entityIndex;
        }

        return (int)(indices - (DrawEntityKey*)_sortIndices.Ptr);
    }


    private unsafe void FillTransformBuffer()
    {
        var src = RenderEcs.Core.TransformView().Ptr;
        foreach (var it in TransformEnumerator())
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
        foreach (var it in SortIndicesSpan)
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
        var span = SortIndicesSpan;
        for (int i = 0; i < span.Length; ++i)
        {
            // (byte)mask | ((uint)depth << 8) | ((uint)queue << 24)
            ref readonly var it = ref span[i];
            var index = new DrawEntityIndex(it.Entity, i);
            var mask = (uint)(byte)it.SortKey;
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

        _sortIndices.ReAlloc(RenderEcs.Core.Capacity, true);
        _transformBuffer.ReAlloc(RenderEcs.Core.Capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _drawIndices.Dispose();
        _sortIndices.Dispose();
        _transformBuffer.Dispose();
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort MakeDepthKeyU16(Vector4 forward, in Vector3 worldPos, Vector2 nearFar, float viewZ)
    {
        const ushort maxValue = 65535;
        var wp = new Vector4(worldPos, 0f);
        var d = Vector4.Dot(forward, wp) - viewZ;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return maxValue;
        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * 65535f + 0.5f);
    }
}