using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.Scene.Template;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneWorld
{
    private readonly World _world;
    private readonly EntityWorld _ecs;
    private readonly GameEntityHub _gameEntities;
    private readonly RenderEntityHub _renderEntities;
    private readonly RenderEntityCore _renderEntityCore;

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly SceneStore _store;
    

    internal SceneWorld(AssetSystem assetSystem, World world, EntityWorld ecs)
    {
        _world = world;
        _ecs = ecs;
        _gameEntities = ecs.GameEntity;
        _renderEntities = ecs.RenderEntity;
        _renderEntityCore = ecs.RenderEntity.Core;

        _assetStore = assetSystem.Store;
        _materialStore = assetSystem.MaterialStore;
        _store = new SceneStore(world, ecs);
    }
    
    public ref Transform GetEntityTransform(RenderEntityId renderEntity) =>
        ref _renderEntityCore.GetTransform(renderEntity).Transform;

    public SceneObjectId CreateSceneObject(string name) => _store.Create(name);


    public EntityTuple SpawnEntity(SceneObjectId id, EntityTemplate template)
    {
        if (template is null || template.GameEntity is null && template.RenderEntity is null)
            throw new ArgumentNullException();

        var ctx = _world.CreateContext();
        var sceneObject = _store.Get(id);

        RenderEntityId renderEntityId = default;
        GameEntityId gameEntityId = default;

        if (template.RenderEntity is { } renderTemplate)
            renderEntityId = RenderEntityFactory.BuildRenderEntity(sceneObject, in ctx, _renderEntities, renderTemplate);

        if (template.GameEntity is { } gameTemplate)
        {
            gameEntityId = GameEntityFactory.BuildGameEntity(sceneObject, _gameEntities, gameTemplate);
            if (gameTemplate.CreateRenderEntity)
            {
                InvalidOpThrower.ThrowIfNot(renderEntityId.IsValid);
                _gameEntities.AddComponent(gameEntityId, new RenderLink { RenderEntityId = renderEntityId });
            }
        }

        return new EntityTuple(gameEntityId, renderEntityId);
    }

}