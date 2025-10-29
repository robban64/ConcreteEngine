#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.World.Data;
using ConcreteEngine.Core.World.Entities;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Core.World.Render;

internal sealed class RenderEntityBus
{
    private const int DefaultCapacity = 128;
    private const int MaxCapacity = 10_000;

    private int _idx = 0;
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];

    private World? _world;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;

    internal RenderEntityBus(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
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

        var idx = _idx;
        foreach (var query in _world.Query<ModelComponent, Transform>())
        {
            ref var model = ref query.Component1;
            ref var transform = ref query.Component2;
            
            //if (model.MaterialKey == default) continue;
            //if (model.Model == default) continue;
            
            _entities[idx++] = new DrawEntity(query.Entity, model.Model, model.MaterialKey, model.DrawCount,
                in transform,
                DrawCommandId.Mesh, DrawCommandQueue.Opaque, PassMask.Default);
        }

        _idx += idx;
    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        FlushWorldEntities(buffer);

        var entitySpan = _entities.AsSpan(0, _idx);

        Span<MaterialId> matSpan = stackalloc MaterialId[7]; // max
        ModelPartView view = default; // ref struct
        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);
        foreach (ref var entity in entitySpan)
        {
            if (entity.Model != prevModel)
                view = _meshTable.GetPartsView(entity.Model);
            if (entity.MaterialKey != prevMatKey)
                _materialTable.ResolveMaterial(entity.MaterialKey, matSpan);

            MatrixMath.CreateModelMatrix(
                entity.Transform.Position,
                entity.Transform.Scale,
                entity.Transform.Rotation,
                out var world
            );

            // stack space for nested loop
            Matrix4x4 model;
            Vector4 v0, v1, v2;
            //

            var meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassMask, entity.DepthKey);
            for (var i = 0; i < view.Locals.Length; i++)
            {
                MatrixMath.MultiplyAffine(in view.Locals[i], in world, out model);
                MatrixMath.CreateNormalMatrix(in model, out v0, out v1, out v2);

                var parts = view.Parts[i];
                var cmd = new DrawCommand(parts.Mesh, new MaterialId(matSpan[parts.MaterialSlot]), parts.DrawCount);
                //meta = new DrawCommandMeta(entity.CommandId, entity.Queue, entity.PassMask, entity.DepthKey);
                buffer.SubmitDraw(cmd, meta, in model, in v0, in v1, in v2);
            }

            prevMatKey = entity.MaterialKey;
            prevModel = entity.Model;
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
    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model, out Vector4 v0,
        out Vector4 v1, out Vector4 v2)
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