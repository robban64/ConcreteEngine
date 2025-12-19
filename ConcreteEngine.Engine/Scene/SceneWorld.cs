using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneWorld
{

    private readonly World _world;
    private readonly EntityWorld _ecs;
    private readonly RenderEntityHub _renderEntityHub;
    private readonly RenderEntityCore _renderEntityCore;

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly SceneStore _store;
    
    public SceneStore Store => _store;

    internal SceneWorld(AssetSystem assetSystem, World world, EntityWorld ecs)
    {
        _world = world;
        _ecs = ecs;
        _renderEntityHub = world.Entities;
        _renderEntityCore = world.Entities.Core;

        _assetStore = assetSystem.StoreImpl;
        _materialStore = assetSystem.MaterialStoreImpl;
        _store = new SceneStore(world);
    }

    public SceneObjectId CreateSceneObject(string name) => Store.Create(name);

    public RenderEntityId SpawnEntity(SceneObjectId id, EntityTemplate template) =>
        Store.SpawnWorldEntity(id, template);

    public ref Transform GetEntityTransform(RenderEntityId renderEntity) =>
        ref _renderEntityCore.GetTransform(renderEntity).Transform;
}