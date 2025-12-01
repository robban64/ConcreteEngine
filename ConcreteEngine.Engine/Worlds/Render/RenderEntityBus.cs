#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Tables;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderEntityBus
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 10_000;

    private int _idx = 0;
    private int _prevIdx = 0;
    private int[] _byEntityId = new int[DefaultCapacity]; //sparse
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    private DrawEntityData[] _entityData = new DrawEntityData[DefaultCapacity];

    private World? _world;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;

    public ModelId CubeId { get; set; }
    public MaterialTagKey EmptyMaterialKey { get; set; }

    internal RenderEntityBus(MeshTable meshTable, MaterialTable materialTable, AnimationTable animationTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
        _animationTable = animationTable;
    }

    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;

    internal void AttachWorld(World world) => _world = world;

    public void Reset()
    {
        _prevIdx = _idx;
        _idx = 0;
    }

    private FrameProfileTimer _timer = new();

    public void CollectEntities(float deltaTime, DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        EnsureFullCapacity(DrawCount);
        buffer.EnsureBufferCapacity(_world.EntityCount + 64);
        buffer.EnsureBoneBuffer(_world.Entities.Animations.Count);

        _timer.Begin();
        CollectModelEntities();

        var ctx = new DrawEntityContext(_idx, _entities, _entityData, _byEntityId);

        AnimatorProcessor.Execute(deltaTime, _animationTable, buffer, ctx);
        SpatialProcessor.Execute(ctx);
        EffectProcessor.Execute(ctx);

        _timer.EndPrint();

        FlushEntities(buffer);
    }

    private void CollectModelEntities()
    {
        if (_entityData.Length == 0 || _entities.Length == 0) return;
        var worldEntities = _world!.Entities;
        var view = worldEntities.Core.GetCoreView();

        if (_entities.Length != _entityData.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        if (view.EntityId.Length != view.Transforms.Length || view.EntityId.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.EntityId.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();

        var idx = 0;

        for (int i = 0; i < len; i++)
        {
            var entityId = view.EntityId[idx];
            ref readonly var source = ref view.Sources[idx];
            ref readonly var transform = ref view.Transforms[idx];
            ref var entity = ref _entities[idx];
            ref var entityData = ref _entityData[idx];

            entity = new DrawEntity(entityId, source);
            entityData.Transform = transform;
            _byEntityId[entityId] = idx;
            idx++;
        }

        foreach (var query in WorldEntities.Query<BoxComponent>())
        {
            var index = _byEntityId[query.Entity];
            if ((uint)index < _entityData.Length)
                _entityData[index].Bounds = query.Component;
        }

        _idx = idx;
    }


    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null || _entities.Length == 0 || _entityData.Length == 0) return;

        FlushWorldEntities(buffer);

        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        MaterialTag materialTag = default;
        ModelPartView modelView = default;
        ReadOnlySpan<MaterialId> matSpan = default;

        // stack space for nested loop


        var len = _idx;

        if ((uint)len > _entities.Length || (uint)len > _entityData.Length)
            throw new IndexOutOfRangeException();

        var bufferContext = buffer.GetDrawUploaderCtx();

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref _entities[i];
            ref readonly var entityData = ref _entityData[i];

            //Temp
            var model = new ModelId(entity.Source.Id);
            if (model != prevModel)
            {
                modelView = _meshTable.GetPartsRefView(model);
                prevModel = model;
            }

            if (entity.Source.MaterialKey != prevMatKey)
            {
                _materialTable.ResolveSubmitMaterial(entity.Source.MaterialKey, out materialTag);
                matSpan = materialTag.AsReadOnlySpan();
                prevMatKey = entity.Source.MaterialKey;
            }


            MatrixMath.CreateModelMatrix(entityData.Transform.Translation, entityData.Transform.Scale,
                entityData.Transform.Rotation, out var world);

            ref var mat0 = ref MemoryMarshal.GetReference(matSpan);

            var parts = modelView.Parts;
            var locals = modelView.Locals;

            var baseMeta = entity.CommandMeta;
            var isAnimated = entity.AnimatedSlot >= 0;
            var animatedSlot = isAnimated ? entity.AnimatedSlot : (short)-1;
            var localLen = int.Min(locals.Length, parts.Length);
            for (var partIdx = 0; partIdx < localLen; partIdx++)
            {
                ref readonly var part = ref parts[partIdx];

                ref var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);

                var isTransparent = materialTag.IsTransparent(part.MaterialSlot);
                var meta = BuildMeta(isTransparent, baseMeta);
                var cmd = new DrawCommand(part.Mesh, mat, drawCount: part.DrawCount, animationSlot: animatedSlot);

                ref var modelTransform = ref bufferContext.UploadDrawAndWrite(cmd, meta);
                ApplyTransform(ref modelTransform, in locals[partIdx], in world, isAnimated);
            }

            prevMatKey = entity.Source.MaterialKey;
            prevModel = model;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyTransform(ref DrawObjectUniform data, in Matrix4x4 local, in Matrix4x4 world,
        bool isAnimated)
    {
        if (isAnimated)
        {
            data.Model = world;
            MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
        }
        else
        {
            MatrixMath.WriteMultiplyAffine(ref data.Model, in local, in world);
            MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DrawCommandMeta BuildMeta(bool isTransparent, DrawEntityCommandMeta m)
    {
        if (!isTransparent)
            return new DrawCommandMeta(m.CommandId, m.Queue, m.Resolver, m.PassMask, m.DepthKey);

        var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
        return new DrawCommandMeta(m.CommandId, DrawCommandQueue.Transparent, m.Resolver, m.PassMask, depthKey);
    }

    private void FlushWorldEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        if (ActiveSkyCount > 0)
        {
            var sky = _world.Sky;
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);

            CreateTransformMatrices(in sky.Transform, out var model, out var normal);
            buffer.SubmitDraw(cmd, meta, in model, in normal);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _meshTable.GetPartsRefView(terrain.Model);

            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);

            CreateTransformMatrices(in Transform.Identity, out var model, out var normal);
            buffer.SubmitDraw(cmd, meta, in model, in normal);
        }

        //Blend = On, SampleAlphaCoverage = Off, DepthWrite = Off
        // Or
        //Blend = Off, SampleAlphaCoverage = On, DepthWrite = Off

        if (_world.Particles.IsActive)
        {
            var particles = _world.Particles;

            var cmd = new DrawCommand(particles.Mesh, particles.Material, instanceCount: particles.ParticleCount);
            var meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, passMask: PassMask.Main);
            buffer.SubmitDrawIdentity(cmd, meta);
        }
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(
            in transform.Translation,
            in transform.Scale,
            in transform.Rotation,
            out model
        );

        MatrixMath.CreateNormalMatrix(in model, out normal);
    }


    private void EnsureFullCapacity(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityData.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityData, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Console.WriteLine($"Entity buffer resize: {newCap}");
    }
}