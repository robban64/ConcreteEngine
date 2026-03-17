using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Editor.Diagnostics;

namespace ConcreteEngine.Engine.Render;
/*
internal sealed class FrameEntityBuffer
{
    public const int MaxCapacity = 1024 * 50;

    public int VisibleCount { get; internal set; }

    internal int[] EntityToVisibleIdx = [];
    internal RenderEntityId[] VisibleEntityIds = [];

    private readonly RenderEntityCore _ecs = Ecs.Render.Core;

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => VisibleEntityIds.AsSpan(0, VisibleCount);
    public ReadOnlySpan<int> GetEntityToVisibleIndex() => EntityToVisibleIdx.AsSpan(0, _ecs.Capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementVisible(RenderEntityId entity, int index)
    {
        var entityIndex = entity.Index();
        EntityToVisibleIdx[entityIndex] = index;
        VisibleEntityIds[index] = entity;
    }

    public void Prepare()
    {
        if (_ecs.Capacity > VisibleEntityIds.Length)
            EnsureCapacity();

        if (EntityToVisibleIdx.Length != VisibleEntityIds.Length)
            throw new InvalidOperationException($"{nameof(FrameEntityBuffer)} array length mismatch");

        if(VisibleCount > 0)
            EntityToVisibleIdx.AsSpan(0, VisibleCount).Clear();

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
}*/