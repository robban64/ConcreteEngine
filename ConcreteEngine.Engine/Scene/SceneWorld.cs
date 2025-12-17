using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneWorld
{
    public SceneStore Store { get; }

    private readonly World _world;
    private readonly WorldEntities _worldEntities;
    private readonly EntityCoreStore _entityCore;

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;

    internal SceneWorld(AssetSystem assetSystem, World world)
    {
        _world = world;
        _worldEntities = world.Entities;
        _entityCore = world.Entities.Core;

        _assetStore = assetSystem.StoreImpl;
        _materialStore = assetSystem.MaterialStoreImpl;
        Store = new SceneStore(world, assetSystem.StoreImpl, assetSystem.MaterialStoreImpl);
    }

    public SceneObjectId CreateSceneObject(string name) => Store.Create(name);

    public EntityId SpawnEntity(SceneObjectId id, EntityTemplate template) =>
        Store.SpawnWorldEntity(id, template);

    public ref Transform GetEntityTransform(EntityId entity) => ref _entityCore.GetTransform(entity);
}