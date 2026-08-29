using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;


internal sealed class RenderEntitySystem : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public int VisibleCount { get; private set; }

    private readonly CameraFrustum _frustum;

    // culled and sorted index
    private NativeArray<DrawEntityIndex> _visibleIndices;
    
    // culled and sorted index
    private NativeArray<TransformUniform> _transforms;

    // draw data
    private readonly Range32[] _passRanges;
    private NativeArray<DrawEntityTicket> _drawTickets;

    internal RenderEntitySystem(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        
        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _drawTickets = NativeArray.Allocate<DrawEntityTicket>(DefaultTicketCapacity);

        _visibleIndices = NativeArray.Allocate<DrawEntityIndex>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    private NativeView<DrawEntityIndex> SortIndices => _visibleIndices.Slice(0, VisibleCount);
    
    public NativeView<TransformUniform> Transforms => _transforms.Slice(0, VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawEntityTicket> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return _drawTickets.Slice(range);
    }

    public void Execute()
    {
        Ensure();
        CullEntities();
        var visibleCount = VisibleCount = BuildVisibleIndices();

        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_visibleIndices.Length);

        SortIndices.Reinterpret<ulong>().AsSpan().Sort();
        SubmitTransforms();
    }

    private void CullEntities()
    {
        foreach (var query in RenderEcs.Core.CullQuery(EntityDrawStatus.Normal))
        {
            var status = query.Policy.Status == EntityDrawStatus.AlwaysVisible;
            var passMask = status
                ? query.Policy.Passes
                : _frustum.Intersects(query.Policy.Passes, in query.Bounds);

            var passes = query.Policy.Passes;
            if (passMask != 0)
            {
                query.VisibilityMask = (byte)passMask;
            }
            else if (passes != passMask)
            {
                query.VisibilityMask = 0;
            }
        }
    }

    private unsafe int BuildVisibleIndices()
    {
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);

        var indices = _visibleIndices.Ptr;
        foreach (var query in RenderEcs.Core.VisibilityBoundsQuery(PassMask.Depth | PassMask.Main | PassMask.Effect))
        {
            ref readonly var center = ref query.Item2.Center;
            var distance = MakeDepthKey(forward,  in center, nearFar, viewZ);

            var queue = RenderEcs.Core.GetDrawPolicy(query.Entity).Queue;
            var drawIndex = DrawEntityIndex.Create(query.Entity, query.Item1, distance, queue);
            *indices++ = drawIndex;
        }

        return (int)(indices - _visibleIndices.Ptr);
    }


    private unsafe void SubmitTransforms()
    {
        var src = Transforms.Ptr;
        foreach (ref readonly var it in SortIndices)
        {
            var entity = it.Entity;
            *src++ = RenderEcs.Core.GetTransformData(entity);
        }
    }


    public unsafe void ReadyDrawCommands()
    {
        if (VisibleCount <= 1) return;

        Array.Clear(_passRanges);

        var heads = stackalloc int[RenderLimits.DrawPassSlots * 2];

        // Count pass tickets
        CountTickets(heads);

        // Count pass ranges
        var total = CountPasses(heads);

        // Create draw tickets
        if (_drawTickets.Length < total)
        {
            var newSize = CapacityUtils.CapacityGrowthToFit(_drawTickets.Length, total);
            _drawTickets.ReAlloc(newSize, true);
        }

        // fill tickets in sorted order
        FillTickets(heads + RenderLimits.DrawPassSlots);
    }


    private unsafe void CountTickets(int* heads)
    {
        foreach (ref readonly var it in SortIndices)
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
        var submitIndex = 0;
        var drawTickets = _drawTickets.Ptr;
        foreach (ref readonly var it in SortIndices)
        {
            var mask = (uint)(byte)it.SortKey;

            DrawEntityTicket ticket;
            ticket.Entity = it.Entity;
            ticket.SubmitIndex = submitIndex++;

            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;

                drawTickets[w] = ticket;
                mask &= mask - 1;
            }
        }
    }

    private void Ensure()
    {
        if (RenderEcs.Core.Capacity == _transforms.Length) return;

        _visibleIndices.ReAlloc(RenderEcs.Core.Capacity, true);
        _transforms.ReAlloc(RenderEcs.Core.Capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _drawTickets.Dispose();
        _visibleIndices.Dispose();
        _transforms.Dispose();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort MakeDepthKey(Vector4 forward, in Vector3 worldPos, Vector2 nearFar, float viewZ)
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

