#region

using System.Diagnostics;
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
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderEntityBus
{
    private int _idx = 0;
    private int _prevIdx = 0;

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

    public void Start()
    {
        if (_world is null) return;

        Validate();

        DrawEntityStore.EnsureDrawEntityData(DrawCount);
        RenderDataContext.EnsureBuffer(_world.EntityCount + 64, _world.Entities.Animations.Count);

        CollectEntities();

        var ctx = new DrawEntityContext(_idx);

        AnimatorProcessor.Execute(ref ctx);
        SpatialProcessor.Execute(ref ctx);
        EffectProcessor.Execute(ref ctx);

        FlushWorldEntities();
        _timer.Begin();
        UploadTransform();
        SubmitDrawCommands();
        _timer.EndPrint();

    }

    private void Validate()
    {
        DrawEntityStore.GetDrawArrays(out var entities, out var entitiesData, out var byEntityId);

        if (entitiesData.Length == 0 || entities.Length == 0) return;
        var worldEntities = _world!.Entities;
        var view = worldEntities.Core.GetCoreView();

        if (entities.Length != entitiesData.Length || entities.Length != byEntityId.Length)
            throw new InvalidOperationException();

        if (view.EntityId.Length != view.Transforms.Length || view.EntityId.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.EntityId.Length;
        if ((uint)len > entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();

    }

    private void CollectEntities()
    {
        var worldEntities = _world!.Entities;
        var idx = 0;
        foreach (var query in worldEntities.CoreQuery())
        {
            DrawEntityProcessor.CollectEntity(idx, query.Entity,  in query.Source);
            DrawEntityProcessor.CollectEntityData(idx, in query.Transform, in query.Box);
            idx++;
        }
        _idx = idx;
    }
    
    private void UploadTransform()
    {
        var entitiesData = DrawEntityStore.EntityData.AsSpan(0, _idx);
        var entities = DrawEntityStore.Entities.AsSpan(0, _idx);

        var len = _idx;
        var writeIdx = 0;

        if ((uint)len > entities.Length || (uint)len > entitiesData.Length)
            throw new IndexOutOfRangeException();
        
        for (var i = 0; i < len; i++)
        {
            writeIdx = SubmitProcessor.ExecuteSubmitTransform(i, writeIdx);
        }
    }


    private void SubmitDrawCommands()
    {
        var entities = DrawEntityStore.Entities;

        if (_world is null || entities.Length == 0) return;

        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        MaterialTag materialTag = default;
        ReadOnlySpan<MaterialId> matSpan = default;
        ReadOnlySpan<MeshPart> parts = default;

        var len = _idx;

        if ((uint)len > entities.Length)
            throw new IndexOutOfRangeException();

        var bufferContext = RenderDataContext.GetDrawUploaderCtx();

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            var baseMeta = entity.FillOut(out var model, out var materialKey, out var animatedSlot);

            if (model != prevModel)
            {
                parts = _meshTable.GetMeshParts(model);
            }

            if (materialKey != prevMatKey)
            {
                _materialTable.ResolveSubmitMaterial(materialKey, out materialTag);
                matSpan = materialTag.AsReadOnlySpan();
            }

            ref var mat0 = ref MemoryMarshal.GetReference(matSpan);
            for (var p = 0; p < parts.Length; p++)
            {
                ref readonly var part = ref parts[p];

                ref var mat = ref Unsafe.Add(ref mat0, part.MaterialSlot);

                var isTransparent = materialTag.IsTransparent(part.MaterialSlot);
                var meta = BuildMeta(isTransparent, baseMeta);
                var cmd = new DrawCommand(part.Mesh, mat, drawCount: part.DrawCount, animationSlot: animatedSlot);
                bufferContext.SubmitDraw(cmd, meta);
            }

            prevModel = model;
            prevMatKey = materialKey;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DrawCommandMeta BuildMeta(bool isTransparent, DrawEntityMeta m)
        {
            if (!isTransparent)
                return new DrawCommandMeta(m.CommandId, m.Queue, m.Resolver, m.PassMask, m.DepthKey);

            var depthKey = (ushort)(ushort.MaxValue - m.DepthKey);
            return new DrawCommandMeta(m.CommandId, DrawCommandQueue.Transparent, m.Resolver, m.PassMask, depthKey);
        }
    }


    private void FlushWorldEntities()
    {
        if (_world is null) return;

        var uploader = RenderDataContext.GetDrawUploaderCtx();
        if (ActiveSkyCount > 0)
        {
            var sky = _world.Sky;
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
            var cmd = new DrawCommand(sky.Mesh, sky.Material);

            CreateTransformMatrices(in sky.Transform, out var model, out var normal);
            uploader.SubmitDrawAndTransform(cmd, meta, in model, in normal);
        }

        if (ActiveTerrainCount > 0)
        {
            var terrain = _world.Terrain;
            var view = _meshTable.GetPartsRefView(terrain.Model);

            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);

            CreateTransformMatrices(in Transform.Identity, out var model, out var normal);
            uploader.SubmitDrawAndTransform(cmd, meta, in model, in normal);
        }

        //Blend = On, SampleAlphaCoverage = Off, DepthWrite = Off
        // Or
        //Blend = Off, SampleAlphaCoverage = On, DepthWrite = Off

        if (_world.Particles.IsActive)
        {
            var particles = _world.Particles;

            var cmd = new DrawCommand(particles.Mesh, particles.Material, instanceCount: particles.ParticleCount);
            var meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Particles, passMask: PassMask.Main);
            uploader.SubmitDrawIdentity(cmd, meta);
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


    /*
    public void FlushEntities(DrawCommandBuffer buffer)
    {
        if (_world is null || _entities.Length == 0 || _entityData.Length == 0) return;

        FlushWorldEntities(buffer);

        var prevModel = new ModelId(-1);
        var prevMatKey = new MaterialTagKey(-1);

        MaterialTag materialTag = default;
        ModelPartView modelView = default;
        ReadOnlySpan<MaterialId> matSpan = default;

        var len = _idx;

        if ((uint)len > _entities.Length || (uint)len > _entityData.Length)
            throw new IndexOutOfRangeException();

        var bufferContext = buffer.GetDrawUploaderCtx();

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref _entities[i];
            ref readonly var entityData = ref _entityData[i];

            if (entity.Source.Model != prevModel)
            {
                modelView = _meshTable.GetPartsRefView(entity.Source.Model);
                prevModel = entity.Source.Model;
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

            var baseMeta = entity.Meta;
            var isAnimated = entity.Source.AnimatedSlot > 0;
            var animatedSlot = entity.Source.AnimatedSlot > 0 ? entity.Source.AnimatedSlot : (ushort)0;
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
            prevModel = entity.Source.Model;
        }
    }
*/
}