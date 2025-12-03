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
    private static int _idx = 0;
    private static int _prevIdx = 0;

    private World? _world;

    public ModelId CubeId { get; set; }
    public MaterialTagKey EmptyMaterialKey { get; set; }

    internal RenderEntityBus()
    {
    }
    
    public void Reset()
    {
        _prevIdx = _idx;
        _idx = 0;
    }

    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;

    internal void AttachWorld(World world) => _world = world;

    private FrameProfileTimer _timer = new();

    public void Start()
    {
        if (_world is null) return;

        Validate();

        DrawEntityStore.EnsureDrawEntityData(DrawCount);
        DrawDataProvider.EnsureBuffer(_world.EntityCount + 64, _world.Entities.Animations.Count);
        
        CollectEntities();
        
        TagCollectedEntities();
        FlushWorldEntities();
        
        DrawAnimatorProcessor.ExecuteAndUpload();
        UploadTransform();
        UploadDrawCommands();
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

    private static void CollectEntities()
    {
        var idx = 0;
        foreach (var query in WorldEntities.CoreQuery())
        {
            DrawEntityCollector.CollectEntity(idx, query.Entity, in query.Source);
            DrawEntityCollector.CollectEntityData(idx, in query.Transform, in query.Box);
            idx++;
        }
        _idx = idx;
    }
    
    private static void TagCollectedEntities()
    {
        DrawEffectProcessor.TagEffectResolvers();
        DrawAnimatorProcessor.TagAnimationSlots();
        DrawSpatialProcessor.TagDepthKeys(_idx);
    }

    private static void UploadTransform()
    {
        var entitiesData = DrawEntityStore.EntityData.AsSpan(0, _idx);
        var entities = DrawEntityStore.Entities.AsSpan(0, _idx);

        var len = _idx;
        var writeIdx = 0;

        if ((uint)len > entities.Length || (uint)len > entitiesData.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            writeIdx = DrawCommandProcessor.ExecuteSubmitTransform(i, writeIdx);
        }
    }

    private static void UploadDrawCommands()
    {
        var entities = DrawEntityStore.Entities;

        var len = _idx;

        if ((uint)len > entities.Length)
            throw new IndexOutOfRangeException();

        MaterialTag materialTag = default;
        var prevMatKey = new MaterialTagKey(-1);

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            var matKey = entity.Source.MaterialKey;
            if (matKey != prevMatKey)
            {
                DrawDataProvider.ResolveMaterial(matKey, out materialTag);
                prevMatKey = matKey;
            }

            DrawCommandProcessor.ExecuteSubmitCommand(i, in entity, in materialTag);
        }
    }

    private void FlushWorldEntities()
    {
        if (_world is null) return;

        var uploader = DrawDataProvider.GetDrawUploaderCtx();
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
            var view = DrawDataProvider.GetPartsRefView(terrain.Model);

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
}