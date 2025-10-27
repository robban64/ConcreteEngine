#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Core.RenderingSystem;

internal sealed class RenderEntityBus
{
    private const int DefaultCapacity = 128;
    private const int MaxCapacity = 10_000;

    private World? _world;

    private readonly ModelRenderRegistry _modelRegistry;

    private int _idx = 0;
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];

    internal RenderEntityBus(ModelRenderRegistry modelRegistry)
    {
        _modelRegistry = modelRegistry;
    }


    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;

    internal void AttachWorld(World world) => _world = world;
    internal bool IsAttached => _world is not null;

    public void CollectEntities()
    {
        if (_world is null) return;

        EnsureCapacity(DrawCount);

        if (ActiveSkyCount > 0)
        {
            _world.Sky.GetDrawEntity(out var skyEntity);
            _entities[_idx++] = skyEntity;
        }

        if (ActiveTerrainCount > 0)
        {
            _world.Terrain.GetDrawEntity(out var terrainEntity);
            _entities[_idx++] = terrainEntity;
        }

        foreach (var query in _world.Query<ModelComponent, Transform>())
        {
            ref var mesh = ref query.Value1;
            ref var transform = ref query.Value2;
            EntityUtility.MakeDrawMesh(in mesh, in transform, out var drawEntity);
            _entities[_idx++] = drawEntity;
        }
    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        var entitySpan = _entities.AsSpan(0, _idx);

        foreach (ref var entity in entitySpan)
        {
            var parts = _modelRegistry.GetParts(entity.Model);
            var cmd = new DrawCommand(parts.MeshId, entity.MaterialId, entity.DrawCount);
            var meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassPassMask, entity.DepthKey);

            MatrixMath.CreateModelMatrix(
                entity.Transform.Position,
                entity.Transform.Scale,
                entity.Transform.Rotation,
                out var model
            );
            MatrixMath.CreateNormalMatrix(in model, out var normal);
            buffer.SubmitDraw(cmd, meta, in model, in normal);
        }
    }

    public void Reset()
    {
        _idx = 0;
    }

    private void EnsureCapacity(int amount)
    {
        if (_entities.Length >= amount) return;
        var newCap = ArrayUtility.CapacityGrowthToFit(amount, Math.Max(amount, 4));

        if (newCap > MaxCapacity)
            ThrowMaxCapacityExceeded();

        Array.Resize(ref _entities, newCap);
    }

    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Entity Buffer exceeded max limit");
}