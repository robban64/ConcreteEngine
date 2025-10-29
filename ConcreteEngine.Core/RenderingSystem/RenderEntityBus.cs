#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Diagnostics.Utility;
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

    private readonly MeshTable _meshTable;

    private int _idx = 0;
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];

    internal RenderEntityBus(MeshTable meshTable)
    {
        _meshTable = meshTable;
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
            _entities[_idx++] = new DrawEntity(query.Entity, model.Model, model.MaterialKey, model.DrawCount, in transform,
                DrawCommandId.Mesh, DrawCommandQueue.Opaque, PassMask.Default);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Test()
    {
        var span = _entities.AsSpan(0, _idx);
        Span<MaterialId> matSpan = stackalloc MaterialId[7];
        foreach (ref var entity in span)
        {
            _world!.EntityMaterials.ResolveMaterial(entity.MaterialKey, matSpan);
        }

    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        FlushWorldEntities(buffer);
        
        var entitySpan = _entities.AsSpan(0, _idx);
        Test();
        
        Span<MaterialId> matSpan = stackalloc MaterialId[7];
        foreach (ref var entity in entitySpan)
        {
            var view = _meshTable.GetPartsView(entity.Model);
            MatrixMath.CreateModelMatrix(
                entity.Transform.Position,
                entity.Transform.Scale,
                entity.Transform.Rotation,
                out var world
            );
            Matrix4x4 model;
            Vector4 v0,v1,v2;

            var materialLength = _world.EntityMaterials.ResolveMaterial(entity.MaterialKey, matSpan);
            var meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassMask, entity.DepthKey);
            for (var i = 0; i < view.Locals.Length; i++)
            {
                MatrixMath.MultiplyAffine(in view.Locals[i], in world, out model);
                MatrixMath.CreateNormalMatrix(in model, out  v0, out  v1, out  v2);

                var parts = view.Parts[i];
                var cmd = new DrawCommand(parts.Mesh, new MaterialId(matSpan[parts.MaterialSlot]), parts.DrawCount);
                //meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassMask, entity.DepthKey);
                buffer.SubmitDraw(cmd, meta, in model, in v0, in v1, in v2);
            }
        }
    }

    private void FlushWorldEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        if (ActiveSkyCount > 0)
        {
            var sky = _world.Sky;
            CreateTransformMatrices(sky.Transform, out var model, out var v0, out var v1, out var v2);
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, PassMask.Main, 0);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);
            buffer.SubmitDraw(cmd, meta, in model, in v0, in v1, in v2);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _meshTable.GetPartsView(terrain.Model);
            
            CreateTransformMatrices(terrain.Transform, out var model, out var v0, out var v1, out var v2);
            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);
            buffer.SubmitDraw(cmd, meta, in model, in v0, in v1, in v2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model, out Vector4 v0, out Vector4 v1,  out Vector4 v2)
    {
        MatrixMath.CreateModelMatrix(
            transform.Position,
            transform.Scale,
            transform.Rotation,
            out model
        );
        
        MatrixMath.CreateNormalMatrix(in model, out v0, out v1, out v2);
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