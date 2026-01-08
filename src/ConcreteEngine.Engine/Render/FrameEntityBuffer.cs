using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;

namespace ConcreteEngine.Engine.Render;

internal sealed class FrameEntityBuffer
{
    public const int MaxCapacity = 1024 * 50;

    public int VisibleCount { get; internal set; }

    private Matrix4x4[] _worldMatrices;
    
    private int[] _entityToVisibleIdx;
    private RenderEntityId[] _visibleEntityIds;

    private readonly RenderEntityCore _ecs;

    public FrameEntityBuffer()
    {
        _ecs = Ecs.Render.Core;

        _visibleEntityIds = new RenderEntityId[_ecs.Capacity];
        _worldMatrices = new Matrix4x4[_ecs.Capacity];
        _entityToVisibleIdx = new int[_ecs.Capacity];
    }

    public RenderEntityCore RenderEcs => _ecs;
    public int EcsCapacity => _ecs.Capacity;

    public ReadOnlySpan<RenderEntityId> VisibleEntities => _visibleEntityIds.AsSpan(0, VisibleCount);
    public ReadOnlySpan<Matrix4x4> WorldMatrices => _worldMatrices.AsSpan(0, _ecs.Capacity);
    public ReadOnlySpan<int> EntityToVisibleIndex => _entityToVisibleIdx.AsSpan(0, _ecs.Capacity);

    public void GetWriteSpans(
        out Span<RenderEntityId> visibleIdsBuffer, 
        out Span<Matrix4x4> worldBuffer, 
        out Span<int> mapBuffer)
    {
        visibleIdsBuffer = _visibleEntityIds.AsSpan();
        worldBuffer = _worldMatrices.AsSpan();
        mapBuffer = _entityToVisibleIdx.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementVisible(RenderEntityId entity, int index)
    {
        _entityToVisibleIdx[entity] = index;
        _visibleEntityIds[index] = entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Matrix4x4 WriteWorldMatrix(RenderEntityId entity) => ref _worldMatrices[entity];


    public void BeginFrame()
    {
        //        DurationProfileTimer.Default2.Begin();
        // DurationProfileTimer.Default2.EndPrint();

        if (_worldMatrices.Length != _visibleEntityIds.Length || _worldMatrices.Length != _entityToVisibleIdx.Length)
            throw new InvalidOperationException($"{nameof(FrameEntityBuffer)} array length mismatch");

        _entityToVisibleIdx.AsSpan(0, _ecs.Count).Fill(-1);

        VisibleCount = 0;

        if (_ecs.Capacity > _visibleEntityIds.Length)
            EnsureCapacity();
    }

    private void EnsureCapacity()
    {
        var newCap = Arrays.CapacityGrowthSafe(_worldMatrices.Length, _ecs.Capacity);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException($"{nameof(FrameEntityBuffer)} Buffer exceeded max limit");

        _visibleEntityIds = new RenderEntityId[newCap];
        _worldMatrices = new Matrix4x4[newCap];
        _entityToVisibleIdx = new int[newCap];

        Logger.LogString(LogScope.World, $"{nameof(FrameEntityBuffer)} buffer resize {newCap}", LogLevel.Warn);
    }
}