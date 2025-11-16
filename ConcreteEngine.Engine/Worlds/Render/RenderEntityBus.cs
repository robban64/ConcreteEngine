#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

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

    public void CollectEntities(in Matrix4x4 viewMat, in ProjectionInfoData projInfo)
    {
        if (_world is null) return;

        EnsureCapacity(DrawCount);

        var selected = WorldActionSlot.SelectedEntityId;

        float near = projInfo.Near, far = projInfo.Far;
        var idx = _idx;
        foreach (var query in _world.Query<ModelComponent, Transform>())
        {
            //Debug.Assert(model.Model != default && model.MaterialKey != default);
            ref var model = ref query.Component1;
            ref var transform = ref query.Component2;

            ref var entity = ref _entities[idx++];


            var depthKey = DepthKeyUtility.MakeDepthKey(in viewMat, in transform.Translation, near, far);

            var meta = new DrawCommandMeta(DrawCommandId.Mesh, DrawCommandQueue.Opaque, DrawCommandResolver.None,
                PassMask.Default, depthKey);

            if (query.Entity == selected)
            {
                meta = meta.WithResolvePass(DrawCommandResolver.Highlight, PassMask.Effect | PassMask.DepthPre);
            }

            entity.Entity = query.Entity;
            entity.Model = model.Model;
            entity.MaterialKey = model.MaterialKey;
            entity.Transform = transform;
            entity.Meta = meta;
        }

        _idx += idx;
    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        buffer.EnsureBufferCapacity(_world.EntityCount + 64);

        FlushWorldEntities(buffer);

        ModelPartView modelView = default; // ref struct
        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        var entitySpan = _entities.AsSpan(0, _idx);
        ReadOnlySpan<MaterialId> matSpan = default;
        MaterialTag tag = default;

        foreach (ref var entity in entitySpan)
        {
            if (entity.Model != prevModel)
            {
                modelView = _meshTable.GetPartsRefView(entity.Model);
                prevModel = entity.Model;
            }

            if (entity.MaterialKey != prevMatKey)
            {
                _materialTable.ResolveSubmitMaterial(entity.MaterialKey, out tag);
                matSpan = tag.AsReadOnlySpan();
                prevMatKey = entity.MaterialKey;
            }

            MatrixMath.CreateModelMatrix(
                entity.Transform.Translation,
                entity.Transform.Scale,
                entity.Transform.Rotation,
                out var world
            );

            // stack space for nested loop
            Matrix4x4 model;
            Vector4 v0, v1, v2;

            var parts = modelView.Parts;
            var locals = modelView.Locals;

            ref var mat0 = ref MemoryMarshal.GetReference(matSpan);

            var baseMeta = entity.Meta;
            for (var i = 0; i < locals.Length; i++)
            {
                ref readonly var part = ref parts[i];
                ref readonly var local = ref locals[i];
                ref readonly var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);

                MatrixMath.MultiplyAffine(in local, in world, out model);
                MatrixMath.CreateNormalMatrix(in model, out v0, out v1, out v2);

                var cmd = new DrawCommand(part.Mesh, mat, part.DrawCount);
                var meta = baseMeta;
                if (tag.IsTransparent(part.MaterialSlot))
                {
                    var depthKey = (ushort)(ushort.MaxValue - meta.DepthKey);
                    meta = meta.WithTransparency(DrawCommandQueue.Transparent, depthKey);
                }

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
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);
            buffer.SubmitDraw(cmd, meta, in model, in v0, in v1, in v2);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _meshTable.GetPartsRefView(terrain.Model);

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
            transform.Translation,
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() =>
        throw new OutOfMemoryException("Entity Buffer exceeded max limit");
}