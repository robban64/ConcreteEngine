using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneStore
{
    private const int DefaultCapacity = 128;

    private static int _idx;
    private static int _handleIdx;

    private SceneObject[] _objects = new SceneObject[DefaultCapacity];
    private SceneObjectHandle[] _handles = new SceneObjectHandle[DefaultCapacity];

    private readonly Dictionary<SceneObjectId, Guid> _toGuid = new(DefaultCapacity);
    private readonly Dictionary<string, SceneObjectId> _byName = new(DefaultCapacity);

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly World _world;
    
    private readonly WorldEntities _worldEntities;
    private readonly EntityCoreStore _entityCore;

    internal SceneStore(World world, AssetStore assetStore, MaterialStore materialStore)
    {
        if (_idx > 0 || _handleIdx > 0) throw new InvalidOperationException();
        _world = world;
        _assetStore = assetStore;
        _materialStore = materialStore;

        _worldEntities = world.Entities;
        _entityCore = world.Entities.Core;
    }


    internal SceneObjectId Create(string name)
    {
        EnsureCapacity(1);

        var index = _idx++;
        var id = new SceneObjectId(_idx, 0);
        if (!string.IsNullOrEmpty(name))
        {
            if (!_byName.TryAdd(name, id))
                throw new InvalidOperationException($"SceneObject with name {name} already exists");
        }

        var guid = Guid.NewGuid();
        _toGuid.Add(id, guid);

        _handles[_handleIdx++] = new SceneObjectHandle(id, index, 1);
        _objects[index] = new SceneObject(id, guid, name);

        return id;
    }

    internal EntityId SpawnWorldEntity(SceneObjectId id, EntityTemplate e)
    {
        
        var ctx = _world.CreateContext();
        var sceneObject = _objects[id - 1];
        CoreComponentBundle coreComponent = default;
        ParticleEmitter? emitter = null;

        if (e.Spatial is { } spatial) coreComponent.Box = spatial.LocalBounds;

        if (e.Model is { } model)
        {
            var materialKey = ctx.MaterialTable.Add(MaterialTagBuilder.FromSpan(model.Materials));
            var kind = e.Animation != null ? EntitySourceKind.AnimatedModel : EntitySourceKind.Model;
            coreComponent.Source = new SourceComponent(model.Model, materialKey, kind);
            sceneObject.HasModel = true;
            
        }
        else if (e.Particle is { } particle)
        {
            if (!ctx.Particles.TryGetEmitter(particle.EmitterName, out emitter))
            {
                emitter = ctx.Particles
                    .CreateEmitter(particle.EmitterName, particle.ParticleCount, in particle.Definition);
            }

            coreComponent.Source = new SourceComponent(emitter.Model, emitter.MaterialKey, EntitySourceKind.Particle);

            sceneObject.HasParticle = true;
        }

        var entity = _worldEntities.AddEntity(in coreComponent);

        if (e.Animation is { } animation)
        {
            if (e.Model is null) throw new InvalidOperationException();
            var component = new AnimationComponent { Animation = animation.Animation, Clip = animation.Clip };
            _worldEntities.AddComponent(entity, component);
            sceneObject.HasAnimation = true;
        }

        if (emitter is not null)
        {
            var component = new ParticleComponent(emitter.EmitterHandle, emitter.Material);
            _worldEntities.AddComponent(entity, component);
        }

        sceneObject.LinkEntity(entity);
        return entity;
    }

    private void ValidateSceneObjectId(SceneObjectId id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value, nameof(id.Value));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(id.Value, _idx, nameof(id.Value));

        var actual = _objects[id];

        if (actual is null)
            throw new InvalidOperationException($"SceneObject: {id} does not exist");
        if (actual.Id != id)
            throw new InvalidOperationException($"SceneObject: {id} does not match actual: {actual}");
    }

    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (len >= _objects.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_objects.Length, len);
            Array.Resize(ref _objects, newSize);

            Logger.LogString(LogScope.World, $"SceneObject: resized {newSize}", LogLevel.Warn);
        }
        
        len = _handleIdx + amount;
        if (len >= _handles.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_handles.Length, len);
            Array.Resize(ref _handles, newSize);

            Logger.LogString(LogScope.World, $"SceneObject Handles: resized {newSize}", LogLevel.Warn);
        }
    }

    private readonly record struct SceneObjectHandle(int SceneObject, int Slot, ushort Gen)
    {
        public bool IsValid => SceneObject > 0 && Slot >= 0 && Gen > 0;

        public bool VerifyGameEntityId(SceneObjectId e) => e.Value == SceneObject && e.Gen == Gen;
    }
}