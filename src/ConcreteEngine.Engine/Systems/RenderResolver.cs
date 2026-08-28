using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

[StructLayout(LayoutKind.Sequential)]
public struct RenderEntityIndex(RenderEntityId entity, uint sortKey)
{
    public RenderEntityId Entity = entity;
    public uint SortKey = sortKey;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawTicket(RenderEntityId entity, int submitIndex)
{
    public RenderEntityId Entity = entity;
    public int SubmitIndex = submitIndex;
}

internal sealed class RenderResolver : IDisposable
{
    private const int DefaultTicketCapacity = 1024 * 4;

    public int VisibleCount { get; private set; }

    private readonly CameraFrustum _frustum;

    private NativeArray<RenderEntityIndex> _visibleIndices;
    private NativeArray<TransformUniform> _transforms;

    private readonly Range32[] _passRanges;
    private NativeArray<DrawTicket> _drawTickets;

    internal RenderResolver(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _passRanges = new Range32[RenderLimits.DrawPassSlots];
        _visibleIndices = NativeArray.Allocate<RenderEntityIndex>(RenderEcs.Core.Capacity);
        _drawTickets = NativeArray.Allocate<DrawTicket>(DefaultTicketCapacity);
        _transforms = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    private NativeView<RenderEntityIndex> SortIndices => _visibleIndices.Slice(0, VisibleCount);

    public NativeView<TransformUniform> Transforms => _transforms.Slice(0, VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<DrawTicket> GetDrawTickets(int passId)
    {
        var range = _passRanges[passId];
        return _drawTickets.Slice(range);
    }

    public void Execute()
    {
        Ensure();

        Cull();
        var visibleCount = VisibleCount = Build();
        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_visibleIndices.Length);

        SortIndices.Reinterpret<ulong>().AsSpan().Sort();
        SubmitTransforms();

    }

    private void Cull()
    {
        foreach (var query in RenderEcs.Core.CullQuery(EntityDrawStatus.Normal, 0))
        {
            var originalMask = query.Policy.Passes;
            var status = query.Policy.Status == EntityDrawStatus.AlwaysVisible;
            var mask = status
                ? originalMask
                : _frustum.Intersects(originalMask, in query.Bounds);

            if (mask != 0)
            {
                query.VisibilityMask = (byte)mask;
            }
            else if (originalMask != mask)
            {
                query.VisibilityMask = 0;
            }
        }
    }

    private unsafe int Build()
    {
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);

        var indices = _visibleIndices.Ptr;
        foreach (var query in RenderEcs.Core.VisibilityBoundsQuery(PassMask.Depth | PassMask.Main | PassMask.Effect))
        {
            ref readonly var center = ref query.Item2.Center;
            var distance = FrustumMath.MakeDepthKey(forward, in center, nearFar, viewZ);

            var mask = query.Item1;
            var queue = RenderEcs.Core.GetDrawPolicy(query.Entity).Queue;
            var sortKey = PackSortKey32(mask, distance, queue);
            RenderEntityIndex drawIndex;
            drawIndex.Entity = query.Entity;
            drawIndex.SortKey = sortKey;
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
        var drawIndex = SortIndices.Ptr;
        var drawIndexEnd = SortIndices.EndPtr;
        while (drawIndex < drawIndexEnd)
        {
            var mask = (uint)(byte)drawIndex->SortKey;
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                ++heads[p];
                mask &= mask - 1;
            }

            ++drawIndex;
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
        var drawTickets = _drawTickets.Ptr;

        var indices = SortIndices;
        var drawIndex = indices.Ptr;
        var drawIndexEnd = indices.EndPtr;
        while (drawIndex < drawIndexEnd)
        {
            var submitIndex = (int)(drawIndex - indices);

            var entity = drawIndex->Entity;
            var mask = (uint)(byte)drawIndex->SortKey;
            DrawTicket ticket;
            ticket.SubmitIndex = submitIndex;
            ticket.Entity = entity;

            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                var w = heads[p]++;

                drawTickets[w] = ticket;
                mask &= mask - 1;
            }

            ++drawIndex;
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
    private static uint PackSortKey32(byte mask, ushort depthKey, DrawQueue queue)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        return mask | ((uint)depth << 8) | ((uint)queue << 24);
    }
}


/*

   private unsafe int CullEntities()
   {
       var nearFar = CameraManager.Instance.Camera.NearFarPlane;
       var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
       var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);

       var indices = _indices.AsView().Ptr;
       foreach (var query in CullQuery())
       {
           var alwaysVisible = query.Policy.Status == EntityDrawStatus.AlwaysVisible;
           var mask = alwaysVisible
               ? query.Policy.Passes
               : _frustum.Intersects(query.Policy.Passes, in query.Item1);

           var originalMask = query.Policy.Passes;
           var queue = query.Policy.Queue;

           if (mask != 0)
           {
               ref readonly var center = ref query.Item1.Center;
               var depthKey = FrustumMath.MakeDepthKey(forward, in center, nearFar, viewZ);
               RenderEntityIndex index;
               index.Entity = query.Entity;
               index.SortKey = PackSortKey32((byte)mask, depthKey, queue);
               *indices++ = index;

               query.VisibilityMask = (byte)mask;
           }
           else if (originalMask != mask)
           {
               query.VisibilityMask = 0;
           }
       }

       return VisibleCount = (int)(indices - _indices.Ptr);
   }

    */