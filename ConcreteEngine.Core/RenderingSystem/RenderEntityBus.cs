#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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

    private readonly ModelRegistry _modelRegistry;

    private int _idx = 0;
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];

    internal RenderEntityBus(ModelRegistry modelRegistry)
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
            ref var model = ref query.Value1;
            ref var transform = ref query.Value2;

            EntityUtility.MakeDrawMesh(model, in transform, out _entities[_idx]);
            _idx++;
        }
    }


    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        var entitySpan = _entities.AsSpan(0, _idx);

        foreach (ref var entity in entitySpan)
        {
            var view = _modelRegistry.GetPartsView(entity.Model);
            MatrixMath.CreateModelMatrix(
                entity.Transform.Position,
                entity.Transform.Scale,
                entity.Transform.Rotation,
                out var world
            );
            Matrix4x4 model;
            Matrix3 normal;
            for (var i = 0; i < view.Locals.Length; i++)
            {
                MatrixMath.MultiplyAffine(in view.Locals[i], in world, out model);
                MatrixMath.CreateNormalMatrix(in model, out normal);

                var parts = view.Parts[i];
                var cmd = new DrawCommand(parts.Mesh, entity.Material, entity.DrawCount);
                var meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassPassMask, entity.DepthKey);
                buffer.SubmitDraw(cmd, meta, in model, in normal);
            }
        }
    }

    /*
    private void ResolveEntity(ModelId modelId, in Transform transform)
    {
        var view = _modelRegistry.GetPartsView(modelId);

        MatrixMath.CreateModelMatrix(xf.Position, xf.Scale, xf.Rotation, out var world);
        for (int i = 0; i < view.Parts.Length; i++)
        {
            ref readonly var part = ref view.Parts[i];
            ref readonly var local = ref view.Locals[i];
            var modelTransform = wo
        }
    }
*/
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