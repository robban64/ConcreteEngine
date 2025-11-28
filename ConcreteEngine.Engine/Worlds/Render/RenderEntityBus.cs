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
using ConcreteEngine.Engine.Worlds.Render.Data;
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
    private int[] _byEntityId = new int[DefaultCapacity];
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    private DrawEntityData[] _entityData = new DrawEntityData[DefaultCapacity];

    private World? _world;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;

    private readonly AnimatorProcessor _animatorProcessor;

    public ModelId CubeId { get; set; }
    public MaterialTagKey EmptyMaterialKey { get; set; }

    internal RenderEntityBus(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
        _animatorProcessor = new AnimatorProcessor(_meshTable);
    }

    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;


    internal void AttachWorld(World world) => _world = world;

    public void Reset()
    {
        _idx = 0;
    }
    private FrameProfileTimer _timer = new();

    public void CollectEntities(float deltaTime,  DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        EnsureCapacity(DrawCount);
        CollectModelEntities();

        var ctx = new RenderFrameContext
        {
            EntitySpan = _entities.AsSpan(0, _idx), EntityByIdSpan = _byEntityId.AsSpan(),
        };
        _animatorProcessor.ProcessAnimations(deltaTime, _world.Entities, buffer, ctx);
    }

    private void CollectModelEntities()
    {
        var worldEntities = _world!.Entities;
        var selected = WorldActionSlot.SelectedEntityId;
        
        var projInfo = RenderDataSlot.ProjectionInfo;
        var viewMatrix = RenderDataSlot.ViewData.ViewMatrix;
        float near = projInfo.Near, far = projInfo.Far;

        var idxCollect = 0;
        foreach (var query in worldEntities.Query<ModelComponent, Transform>())
        {
            //Debug.Assert(model.Model != default && model.MaterialKey != default);
            ref var model = ref query.Component1;
            ref var transform = ref query.Component2;

            ref var entity = ref _entities[idxCollect];
            ref var entityData = ref _entityData[idxCollect];
            entityData.Transform = transform;
            entityData.Bounds = worldEntities.BoundingBoxes.GetById(query.Entity).Box;

            _byEntityId[entity.Entity] = idxCollect++;

            var depthKey = DepthKeyUtility.MakeDepthKey(in viewMatrix,  transform.Translation, near, far);

            var meta = new DrawEntityCommandMeta(DrawCommandId.Model, DrawCommandQueue.Opaque, DrawCommandResolver.None,
                PassMask.Default, depthKey);

            if (query.Entity == selected)
            {
                meta = meta.WithResolvePass(DrawCommandResolver.Highlight, PassMask.Effect | PassMask.DepthPre);
            }

            entity.Entity = query.Entity;
            entity.Model = model.Model;
            entity.MaterialKey = model.MaterialKey;
            entity.CommandMeta = meta;
        }
        _idx = idxCollect;

    }

    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null) return;

        buffer.EnsureBufferCapacity(_world.EntityCount + 64);

        FlushWorldEntities(buffer);

        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        MaterialTag materialTag = default;
        ModelPartView modelView = default;
        ReadOnlySpan<MaterialId> matSpan = default;

        // stack space for nested loop
        DrawObjectUniform drawData = default;

        var entitySpan = _entities.AsSpan(0, _idx);

        foreach (ref var entity in entitySpan)
        {
            if (entity.Model != prevModel)
            {
                modelView = _meshTable.GetPartsRefView(entity.Model);
                prevModel = entity.Model;
            }

            if (entity.MaterialKey != prevMatKey)
            {
                _materialTable.ResolveSubmitMaterial(entity.MaterialKey, out materialTag);
                matSpan = materialTag.AsReadOnlySpan();
                prevMatKey = entity.MaterialKey;
            }

            Matrix4x4 world = default;
            MatrixMath.CreateModelMatrix(in entity.Transform.Translation, in entity.Transform.Scale,
                in entity.Transform.Rotation, out world);

            ref var mat0 = ref MemoryMarshal.GetReference(matSpan);

            var parts = modelView.Parts;
            var locals = modelView.Locals;

            var baseMeta = entity.Meta;
            var isAnimated = entity.IsAnimated;
            int len = int.Min(locals.Length, parts.Length);
            for (var i = 0; i < len; i++)
            {
                ref readonly var part = ref parts[i];

                ref var draw = ref Unsafe.AsRef(ref drawData);
                ref var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);
                var meta = BuildMeta(ref Unsafe.AsRef(ref materialTag), part.MaterialSlot, baseMeta);
                var cmd = new DrawCommand(part.Mesh, mat, part.DrawCount);

                ApplyTransform(ref draw, in locals[i], in world, isAnimated);
                buffer.SubmitDraw(cmd, meta, ref draw);
            }

            prevMatKey = entity.MaterialKey;
            prevModel = entity.Model;
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
            MatrixMath.MultiplyAffine(in local, in world, out data.Model);
            MatrixMath.CreateNormalMatrix(in data.Model, out data.Normal);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DrawEntityCommandMeta BuildMeta(ref MaterialTag tag, int slot, DrawEntityCommandMeta meta)
    {
        if (!tag.IsTransparent(slot)) return meta;
        var depthKey = (ushort)(ushort.MaxValue - meta.DepthKey);
        
        return meta.WithTransparency(DrawCommandQueue.Transparent, depthKey);
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
            buffer.SubmitNonTransformDraw(cmd, meta);
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


    private void EnsureCapacity(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityData.Length);

        if (_entities.Length >= amount) return;
        var newCap = ArrayUtility.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityData, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Console.WriteLine($"Entity buffer resize: {newCap}");
    }
}