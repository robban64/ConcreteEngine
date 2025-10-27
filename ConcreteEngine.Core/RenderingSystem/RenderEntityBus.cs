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
using ConcreteEngine.Renderer.Definitions;
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

    public void Reset()
    {
        _idx = 0;
    }

    public void CollectEntities()
    {
        if (_world is null) return;

        EnsureCapacity(DrawCount);

        foreach (var query in _world.Query<ModelComponent, Transform>())
        {
            ref var model = ref query.Value1;
            ref var transform = ref query.Value2;

            EntityUtility.MakeDrawMesh(query.Entity, model.Model, model.DrawCount, in transform, out _entities[_idx]);
            _idx++;
        }
    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        FlushWorldEntities(buffer);
        
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

            var materials = _world.EntityMaterials.GetMaterialIds(entity.Entity);
            var meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassMask, entity.DepthKey);
            for (var i = 0; i < view.Locals.Length; i++)
            {
                MatrixMath.MultiplyAffine(in view.Locals[i], in world, out model);
                MatrixMath.CreateNormalMatrix(in model, out normal);

                var parts = view.Parts[i];
                var cmd = new DrawCommand(parts.Mesh, materials[parts.MaterialSlot], parts.DrawCount);
                buffer.SubmitDraw(cmd, meta, in model, in normal);
            }
        }
    }

    private void FlushWorldEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        if (ActiveSkyCount > 0)
        {
            var sky = _world.Sky;
            CreateTransformMatrices(sky.Transform, out var model, out var norm);
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, PassMask.Main, 0);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);
            buffer.SubmitDraw(cmd, meta, in model, in norm);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _modelRegistry.GetPartsView(terrain.Model);
            
            CreateTransformMatrices(terrain.Transform, out var model, out var norm);
            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);
            buffer.SubmitDraw(cmd, meta, in model, in norm);
        }
    }

    private void CreateTransformMatrices(Transform transform, out Matrix4x4 model, out Matrix3 normal)
    {
        MatrixMath.CreateModelMatrix(
            transform.Position,
            transform.Scale,
            transform.Rotation,
            out model
        );
        MatrixMath.CreateNormalMatrix(in model, out normal);
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