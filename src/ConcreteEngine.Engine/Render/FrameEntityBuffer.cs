using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Render;

internal sealed class FrameEntityBuffer
{
    public const int MaxCapacity = 1024 * 50;

    public int VisibleCount { get; internal set; }

    internal int[] EntityToVisibleIdx = [];
    internal RenderEntityId[] VisibleEntityIds = [];

    private readonly RenderEntityCore _ecs;

    public FrameEntityBuffer()
    {
        _ecs = Ecs.Render.Core;
    }

    public RenderEntityCore RenderEcs => _ecs;
    public int EcsCapacity => _ecs.Capacity;

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => VisibleEntityIds.AsSpan(0, VisibleCount);
    public ReadOnlySpan<int> GetEntityToVisibleIndex() => EntityToVisibleIdx.AsSpan(0, _ecs.Capacity);

    public void GetWriteSpans(
        out Span<RenderEntityId> visibleIdsBuffer,
        out Span<int> mapBuffer)
    {
        visibleIdsBuffer = VisibleEntityIds.AsSpan();
        mapBuffer = EntityToVisibleIdx.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementVisible(RenderEntityId entity, int index)
    {
        EntityToVisibleIdx[entity] = index;
        VisibleEntityIds[index] = entity;
    }

    public void Prepare()
    {
        if (_ecs.Capacity > VisibleEntityIds.Length)
            EnsureCapacity();

        if (EntityToVisibleIdx.Length != VisibleEntityIds.Length)
            throw new InvalidOperationException($"{nameof(FrameEntityBuffer)} array length mismatch");

        EntityToVisibleIdx.AsSpan(0, _ecs.Count).Fill(-1);

        VisibleCount = 0;
    }

    private void EnsureCapacity()
    {
        var len = EntityToVisibleIdx.Length;
        var newCap = Arrays.CapacityGrowthSafe(len, _ecs.Capacity);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException($"{nameof(FrameEntityBuffer)} Buffer exceeded max limit");

        VisibleEntityIds = new RenderEntityId[newCap];
        EntityToVisibleIdx = new int[newCap];

        if (len > 0)
            Logger.LogString(LogScope.World, $"{nameof(FrameEntityBuffer)} buffer resize {newCap}", LogLevel.Warn);
    }
}