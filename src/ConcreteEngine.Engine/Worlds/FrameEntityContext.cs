using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Render.Data;

namespace ConcreteEngine.Engine.Worlds;

internal sealed class FrameEntityContext
{
    private const int MaxCapacity = 1024 * 50; // sanity check

    public int VisibleCount { get; internal set; }

    private RenderEntityId[] _entityIndices;
    private Matrix4x4[] _entityWorld;
    
    private readonly RenderEntityCore _ecs;

    public FrameEntityContext()
    {
        _ecs = Ecs.Render.Core;
        _entityIndices = new RenderEntityId[_ecs.Capacity];
        _entityWorld = new Matrix4x4[_ecs.Capacity];
    }

    public RenderEntityCore RenderEcs => _ecs;
    public int EcsCapacity => _ecs.Capacity;


    public ReadOnlySpan<RenderEntityId> VisibleEntitySpan => _entityIndices.AsSpan(0, VisibleCount);
    public ReadOnlySpan<Matrix4x4> EntityWorldSpan => _entityWorld.AsSpan();
    
    internal Span<RenderEntityId> GetRenderEntitySpan() => _entityIndices.AsSpan();
    internal Span<Matrix4x4> GetEntityWorldSpan() => _entityWorld.AsSpan();

    
    public void BeginFrame()
    {
        EnsureCapacity();
        _entityIndices.AsSpan(0, VisibleCount).Clear();
        VisibleCount = 0;
    }

    private void EnsureCapacity()
    {
        InvalidOpThrower.ThrowIf(_entityWorld.Length != _entityIndices.Length);

        if (_entityWorld.Length >= _ecs.Capacity) return;
        var newCap = Arrays.CapacityGrowthSafe(_entityWorld.Length, _ecs.Capacity);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException($"{nameof(FrameEntityContext)} Buffer exceeded max limit");

        _entityIndices = new RenderEntityId[newCap];
        _entityWorld = new Matrix4x4[newCap];

        Logger.LogString(LogScope.World, $"{nameof(FrameEntityContext)} buffer resize {newCap}", LogLevel.Warn);
    }

}